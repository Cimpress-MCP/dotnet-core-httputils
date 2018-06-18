using System;
using System.Threading;
using System.Threading.Tasks;
using Cimpress.Extensions.Http.Caching.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

namespace Cimpress.Extensions.Http.Caching.Redis
{
    /// <summary>
    /// Extension methods for an <see cref="IDistributedCache"/>.
    /// </summary>
    internal static class DistributedCacheExtensions
    {
        /// <summary>
        /// Tries to get the data from cache, that is, ignoring all exceptions.
        /// </summary>
        /// <param name="cache">The distributed cache.</param>
        /// <param name="key">The key to retrieve from the cache.</param>
        /// <param name="cancellationToken">Optional cancellation token to avoid waiting for the cache for too long in case the cache cannot be reached.</param>
        /// <returns>The data of the cache entry, or null if not found or on any error.</returns>
        public static async Task<CacheData> TryGetAsync(this IDistributedCache cache, string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var binaryValue = await cache.GetAsync(key, cancellationToken);
                return binaryValue.Deserialize();
            }
            catch (Exception)
            {
                // ignore all exceptions; return null
                return null;
            }
        }

        /// <summary>
        /// Tries to set a new value to the cache, that is, ignoring all exceptions.
        /// </summary>
        /// <param name="cache">The distributed cache.</param>
        /// <param name="key">The key for this cache entry.</param>
        /// <param name="value">The value of this cache entry.</param>
        /// <param name="absoluteExpirationRelativeToNow">Expiration relative to now.</param>
        /// <param name="cancellationToken">Optional cancellation token to avoid waiting for the cache for too long in case the cache cannot be reached.</param>
        /// <returns>A task, when completed, has tried to put the entry into the cache.</returns>
        public static async Task TrySetAsync(this IDistributedCache cache, string key, CacheData value, TimeSpan absoluteExpirationRelativeToNow, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var binaryValue = value.Serialize();
                var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow };
                await cache.SetAsync(key, binaryValue, options, cancellationToken);
            }
            catch (Exception)
            {
                // ignore all exceptions
            }
        }
    }
}