using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Cimpress.Extensions.Http.Caching.InMemory
{
    /// <summary>
    /// Tries to retrieve the result from an InMemory cache, and if that's not available, gets the value from the underlying handler and caches that result.
    /// </summary>
    public class InMemoryCacheHandler : DelegatingHandler
    {
        private readonly IDictionary<HttpStatusCode, TimeSpan> cacheExpirationPerHttpResponseCode;
        private readonly IMemoryCache responseCache;

        /// <summary>
        /// Create a new InMemoryCacheHandler.
        /// </summary>
        /// <param name="innerHandler">The inner handler to retrieve the content from on cache misses.</param>
        /// <param name="cacheExpirationPerHttpResponseCode">A mapping of HttpStatusCode to expiration times. If unspecified takes a default value.</param>
        public InMemoryCacheHandler(HttpMessageHandler innerHandler, IDictionary<HttpStatusCode, TimeSpan> cacheExpirationPerHttpResponseCode = null)
            : this(innerHandler, cacheExpirationPerHttpResponseCode, new MemoryCache(new MemoryCacheOptions())) {}

        /// <summary>
        /// Used for injecting an IMemoryCache for unit testing purposes.
        /// </summary>
        /// <param name="innerHandler">The inner handler to retrieve the content from on cache misses.</param>
        /// <param name="cacheExpirationPerHttpResponseCode">A mapping of HttpStatusCode to expiration times. If unspecified takes a default value.</param>
        /// <param name="cache">The cache to be used.</param>
        internal InMemoryCacheHandler(HttpMessageHandler innerHandler, IDictionary<HttpStatusCode, TimeSpan> cacheExpirationPerHttpResponseCode, IMemoryCache cache) : base(innerHandler ?? new HttpClientHandler())
        {
            this.cacheExpirationPerHttpResponseCode = cacheExpirationPerHttpResponseCode ?? new Dictionary<HttpStatusCode, TimeSpan>();
            responseCache = cache ?? new MemoryCache(new MemoryCacheOptions());
        }

        /// <summary>
        /// Tries to get the value from the cache, and only calls the delegating handler on cache misses.
        /// </summary>
        /// <returns>The HttpResponseMessage from cache, or a newly invoked one.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // gets the data from cache, and returns the data if it's a cache hit
            CacheData cachedData;
            if (request.Method == HttpMethod.Get && responseCache.TryGetValue(request.RequestUri, out cachedData))
            {
                return PrepareCachedEntry(request, cachedData);
            }

            // cache misses need to ask the inner handler for an actual response
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            // puts the retrieved response into the cache and returns the cached entry
            if (request.Method == HttpMethod.Get)
            {
                CacheData entry = await response.ToCacheEntry();
                TimeSpan absoluteExpirationRelativeToNow = response.StatusCode.GetAbsoluteExpirationRelativeToNow(cacheExpirationPerHttpResponseCode);
                responseCache.Set(request.RequestUri, entry, absoluteExpirationRelativeToNow);
                return PrepareCachedEntry(request, entry);
            }

            // returns the original response
            return response;
        }

        /// <summary>
        /// Prepares the cached entry to be consumed by the caller, notably by setting the content.
        /// </summary>
        /// <param name="request">The request that invoked retrieving this response and need to be attached to the response.</param>
        /// <param name="cachedData">The cache data that hold the data.</param>
        /// <returns>A valid HttpResponseMessage that can be consumed by the caller of this message handler.</returns>
        private static HttpResponseMessage PrepareCachedEntry(HttpRequestMessage request, CacheData cachedData)
        {
            HttpResponseMessage copy = cachedData.CachableResponse.CopyCachable();
            copy.Content = new ByteArrayContent(cachedData.Data);
            copy.RequestMessage = request;
            return copy;
        }
    }
}
