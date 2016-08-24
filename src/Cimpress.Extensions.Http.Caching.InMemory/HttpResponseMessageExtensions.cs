using System.Net.Http;
using System.Threading.Tasks;

namespace Cimpress.Extensions.Http.Caching.InMemory
{
    /// <summary>
    /// Extension methods of the HttpResponseMessage that are related to the caching functionality.
    /// </summary>
    internal static class HttpResponseMessageExtensions
    {
        /// <summary>
        /// Takes an HttpResponseMessage and converts that to a <see cref="CacheData"/>.
        /// </summary>
        /// <param name="response">The response to put into the cache.</param>
        /// <returns>A cache entry that can be placed into the cache.</returns>
        public static async Task<CacheData> ToCacheEntry(this HttpResponseMessage response)
        {
            var data = await response.Content.ReadAsByteArrayAsync();
            var copy = response.CopyCachable();
            var entry = new CacheData(data, copy);
            return entry;
        }

        /// <summary>
        /// Creates a copy of the HttpResponseMessage excluding non-cacheable data such as the content stream or request based data such as the HttpRequestMessage itself.
        /// </summary>
        /// <param name="response">The response to copy.</param>
        /// <returns>A copy of the response, excluding non-cacheable data.</returns>
        public static HttpResponseMessage CopyCachable(this HttpResponseMessage response)
        {
            var responseCopy = new HttpResponseMessage { ReasonPhrase = response.ReasonPhrase, StatusCode = response.StatusCode, Version = response.Version };
            foreach (var h in response.Headers)
            {
                responseCopy.Headers.Add(h.Key, h.Value);
            }
            return responseCopy;
        }
    }
}