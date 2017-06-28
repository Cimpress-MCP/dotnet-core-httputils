using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cimpress.Extensions.Http.Caching.Abstractions;
using Cimpress.Extensions.Http.Caching.Redis.Microsoft;
using Microsoft.Extensions.Caching.Distributed;

namespace Cimpress.Extensions.Http.Caching.Redis
{
    /// <summary>
    /// Tries to retrieve the result from an InMemory cache, and if that's not available, gets the value from the underlying handler and caches that result.
    /// </summary>
    public class RedisCacheHandler : DelegatingHandler
    {
        public IStatsProvider StatsProvider { get; }
        private readonly IDictionary<HttpStatusCode, TimeSpan> cacheExpirationPerHttpResponseCode;
        private readonly IDistributedCache responseCache;

        /// <summary>
        /// Used for injecting an IMemoryCache for unit testing purposes.
        /// </summary>
        /// <param name="innerHandler">The inner handler to retrieve the content from on cache misses.</param>
        /// <param name="cacheExpirationPerHttpResponseCode">A mapping of HttpStatusCode to expiration times. If unspecified takes a default value.</param>
        /// <param name="options">Options to use to connect to Redis.</param>
        /// /// <param name="statsProvider">An <see cref="IStatsProvider"/> that records statistic information about the caching behavior.</param>
        public RedisCacheHandler(HttpMessageHandler innerHandler, IDictionary<HttpStatusCode, TimeSpan> cacheExpirationPerHttpResponseCode, RedisCacheOptions options,
            IStatsProvider statsProvider = null) : this(innerHandler, cacheExpirationPerHttpResponseCode, new RedisCache(options), statsProvider) {}

        /// <summary>
        /// Used internally only for unit testing.
        /// </summary>
        /// <param name="innerHandler">The inner handler to retrieve the content from on cache misses.</param>
        /// <param name="cacheExpirationPerHttpResponseCode">A mapping of HttpStatusCode to expiration times. If unspecified takes a default value.</param>
        /// <param name="cache">The distributed cache to use.</param>
        /// /// <param name="statsProvider">An <see cref="IStatsProvider"/> that records statistic information about the caching behavior.</param>
        internal RedisCacheHandler(HttpMessageHandler innerHandler, IDictionary<HttpStatusCode, TimeSpan> cacheExpirationPerHttpResponseCode, IDistributedCache cache,
            IStatsProvider statsProvider = null) : base(innerHandler ?? new HttpClientHandler())
        {
            this.StatsProvider = statsProvider ?? new StatsProvider(nameof(RedisCacheHandler));
            this.cacheExpirationPerHttpResponseCode = cacheExpirationPerHttpResponseCode ?? new Dictionary<HttpStatusCode, TimeSpan>();
            responseCache = cache;
        }

        /// <summary>
        /// Tries to get the value from the cache, and only calls the delegating handler on cache misses.
        /// </summary>
        /// <returns>The HttpResponseMessage from cache, or a newly invoked one.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = request.Method + request.RequestUri.ToString();
            // gets the data from cache, and returns the data if it's a cache hit
            if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Head)
            {
                var data = await responseCache.TryGetAsync(key);
                if (data != null)
                {
                    var cachedResponse = request.PrepareCachedEntry(data);
                    StatsProvider.ReportCacheHit(cachedResponse.StatusCode);
                    return cachedResponse;
                }
            }

            // cache misses need to ask the inner handler for an actual response
            var response = await base.SendAsync(request, cancellationToken);

            // puts the retrieved response into the cache and returns the cached entry
            if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Head)
            {
                var absoluteExpirationRelativeToNow = response.StatusCode.GetAbsoluteExpirationRelativeToNow(cacheExpirationPerHttpResponseCode);

                StatsProvider.ReportCacheMiss(response.StatusCode);

                if (TimeSpan.Zero != absoluteExpirationRelativeToNow)
                {
                    var entry = await response.ToCacheEntry();
                    await responseCache.TrySetAsync(key, entry, absoluteExpirationRelativeToNow);
                    return request.PrepareCachedEntry(entry);
                }
            }

            // returns the original response
            return response;
        }
    }
}
