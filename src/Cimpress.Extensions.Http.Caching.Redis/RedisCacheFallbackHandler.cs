using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cimpress.Extensions.Http.Caching.Abstractions;
using Cimpress.Extensions.Http.Caching.Redis.Microsoft;
using Microsoft.Extensions.Caching.Distributed;

namespace Cimpress.Extensions.Http.Caching.Redis
{
    /// <summary>
    /// Tries to retrieve the result from a Redis cache, and if that's not available, gets the value from the underlying handler and caches that result.
    /// </summary>
    public class RedisCacheFallbackHandler : DelegatingHandler
    {
        public IStatsProvider StatsProvider { get; }
        private readonly TimeSpan maxTimeout;
        private readonly TimeSpan cacheDuration;
        private readonly IDistributedCache responseCache;
        internal const string CacheFallbackKeyPrefix = "cfb";

        /// <summary>
        /// Used for injecting an IMemoryCache for unit testing purposes.
        /// </summary>
        /// <param name="innerHandler">The inner handler to retrieve the content from on cache misses.</param>
        /// <param name="maxTimeout">The maximum timeout to wait for the service to respond.</param>
        /// <param name="cacheDuration">The maximum time span the item should be remained in the cache if not renewed before then.</param>
        /// <param name="options">Options to use to connect to Redis.</param>
        /// /// <param name="statsProvider">An <see cref="IStatsProvider"/> that records statistic information about the caching behavior.</param>
        public RedisCacheFallbackHandler(HttpMessageHandler innerHandler, TimeSpan maxTimeout, TimeSpan cacheDuration, RedisCacheOptions options,
            IStatsProvider statsProvider = null) : this(innerHandler, maxTimeout, cacheDuration, new RedisCache(options), statsProvider) {}

        /// <summary>
        /// Used internally only for unit testing.
        /// </summary>
        /// <param name="innerHandler">The inner handler to retrieve the content from on cache misses.</param>
        /// <param name="maxTimeout">The maximum timeout to wait for the service to respond.</param>
        /// <param name="cacheDuration">The maximum time span the item should be remained in the cache if not renewed before then.</param>
        /// <param name="cache">The distributed cache to use.</param>
        /// /// <param name="statsProvider">An <see cref="IStatsProvider"/> that records statistic information about the caching behavior.</param>
        internal RedisCacheFallbackHandler(HttpMessageHandler innerHandler, TimeSpan maxTimeout, TimeSpan cacheDuration, IDistributedCache cache,
            IStatsProvider statsProvider = null) : base(innerHandler ?? new HttpClientHandler())
        {
            this.StatsProvider = statsProvider ?? new StatsProvider(nameof(RedisCacheHandler));
            this.maxTimeout = maxTimeout;
            this.cacheDuration = cacheDuration;
            responseCache = cache;
        }

        /// <summary>
        /// Tries to get the value from the cache, and only calls the delegating handler on cache misses.
        /// </summary>
        /// <returns>The HttpResponseMessage from cache, or a newly invoked one.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // only handle GET methods
            if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var key = CacheFallbackKeyPrefix + request.Method + request.RequestUri;

            // start 3 tasks
            var httpSendTask = base.SendAsync(request, cancellationToken);
            var timeoutTask = Task.Delay(maxTimeout, cancellationToken);
            var cacheTask = responseCache.TryGetAsync(key);

            // ensure the send task completes (or the timeout task, whatever comes first).
            var firstCompletedTask = await Task.WhenAny(httpSendTask, timeoutTask);

            // timeout occurred
            if (firstCompletedTask == timeoutTask)
            {
                var data = await ExtractCachedResponse(request, cacheTask);
                if (data != null)
                {
                    // update the cache after the http task eventually completes, without awaiting it
                    var cacheResultTask = httpSendTask.ContinueWith(t => SaveToCache(t.Result, key), TaskContinuationOptions.OnlyOnRanToCompletion);

                    return data;
                }
            }

            // we got it from the HTTP data directly, or it wasn't in the cache and got it after the max timeout value; save that result to the cache and return it
            var response = await httpSendTask;

            // try to save it to the cache
            var entry = await SaveToCache(response, key);

            // when successful, return that value
            if (entry != null)
            {
                StatsProvider.ReportCacheMiss(response.StatusCode);
                return request.PrepareCachedEntry(entry);
            }

            // when unsuccessful, try to get it from the cache, which was the last successful invocation
            var cachedResponse = await ExtractCachedResponse(request, cacheTask);
            if (cachedResponse != null)
            {
                return cachedResponse;
            }

            StatsProvider.ReportCacheMiss(response.StatusCode);

            return response;
        }

        private async Task<HttpResponseMessage> ExtractCachedResponse(HttpRequestMessage request, Task<CacheData> cacheTask)
        {
            // get from cache
            var data = await cacheTask;

            // it's in the cache, return that result
            if (data != null)
            {

                // get the data from the cache
                var cachedResponse = request.PrepareCachedEntry(data);
                StatsProvider.ReportCacheHit(cachedResponse.StatusCode);
                return cachedResponse;
            }

            return null;
        }

        private async Task<CacheData> SaveToCache(HttpResponseMessage response, string key)
        {
            if ((int)response.StatusCode < 500 && TimeSpan.Zero != cacheDuration)
            {
                var entry = await response.ToCacheEntry();
                await responseCache.TrySetAsync(key, entry, cacheDuration);
                return entry;
            }

            return null;
        }
    }
}
