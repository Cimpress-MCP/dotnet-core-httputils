using System.Net.Http;
using System.Threading.Tasks;
using Cimpress.Extensions.Http.Caching.Abstractions;

namespace Cimpress.Extensions.Http.Caching.Redis
{
    /// <summary>
    /// Extension methods of the HttpResponseMessage that are related to the caching functionality.
    /// </summary>
    public static class HttpResponseMessageExtensions
    {
        /// <summary>
        /// Takes an HttpResponseMessage and converts that to a <see cref="CacheData"/>.
        /// </summary>
        /// <param name="response">The response to put into the cache.</param>
        /// <returns>A cache entry that can be placed into the cache.</returns>
        public static async Task<byte[]> ToCacheEntry(this HttpResponseMessage response)
        {
            var data = await response.Content.ReadAsByteArrayAsync();
            var copy = response.CopyCachable();
            var entry = new SerializableCacheData(data, copy);
            return entry.Serialize();
        }
    }
}