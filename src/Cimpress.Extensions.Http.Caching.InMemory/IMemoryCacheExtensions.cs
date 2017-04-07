using System;
using System.Threading.Tasks;
using Cimpress.Extensions.Http.Caching.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace Cimpress.Extensions.Http.Caching.InMemory
{
    /// <summary>
    /// Extension methods for an <see cref="IMemoryCache"/>.
    /// </summary>
    internal static class IMemoryCacheExtensions
    {
        /// <summary>
        /// Tries to get the data from cache, that is, ignoring all exceptions.
        /// </summary>
        /// <param name="cache">The in memory cache.</param>
        /// <param name="key">The key to retrieve from the cache.</param>
        /// <returns>The data of the cache entry, or null if not found or on any error.</returns>
        public static async Task<CacheData> TryGetAsync(this IMemoryCache cache, string key)
        {
            try
            {
                byte[] binaryData = null;
                if (cache.TryGetValue(key, out binaryData))
                {
                    await Task.FromResult(true);
                    return binaryData.Deserialize();
                }
                return null;
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
        /// <param name="cache">The in memory cache.</param>
        /// <param name="key">The key for this cache entry.</param>
        /// <param name="value">The value of this cache entry.</param>
        /// <param name="absoluteExpirationRelativeToNow">Expiration relative to now.</param>
        /// <returns>A task, when completed, has tried to put the entry into the cache.</returns>
        public static Task TrySetAsync(this IMemoryCache cache, string key, CacheData value, TimeSpan absoluteExpirationRelativeToNow)
        {
            try
            {
                cache.Set(key, value.Serialize(), absoluteExpirationRelativeToNow);
                return Task.FromResult(true);
            }
            catch (Exception)
            {
                // ignore all exceptions
                return Task.FromResult(false);
            }
        }
    }
}