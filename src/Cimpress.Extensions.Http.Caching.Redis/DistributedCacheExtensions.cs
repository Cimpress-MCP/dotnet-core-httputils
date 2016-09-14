using System;
using System.Threading.Tasks;
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
        /// <returns>The data of the cache entry, or null if not found or on any error.</returns>
        public static async Task<byte[]> TryGetAsync(this IDistributedCache cache, string key)
        {
            try
            {
                return await cache.GetAsync(key);
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
        /// <param name="options">Cache options for the entry.</param>
        /// <returns>A task, when completed, has tried to put the entry into the cache.</returns>
        public static async Task TrySetAsync(this IDistributedCache cache, string key, byte[] value, DistributedCacheEntryOptions options)
        {
            try
            {
                await cache.SetAsync(key, value, options);
            }
            catch (Exception)
            {
                // ignoer all exceptions
            }
        }
    }
}