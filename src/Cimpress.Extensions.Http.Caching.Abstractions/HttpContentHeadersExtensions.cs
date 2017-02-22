using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Cimpress.Extensions.Http.Caching.Abstractions
{
    /// <summary>
    /// Extension methods of the HttpResponseMessage that are related to the caching functionality.
    /// </summary>
    public static class HttpContentHeadersExtensions
    {
        /// <summary>
        /// Creates a copy of the HttpContentHeaders for caching
        /// </summary>
        /// <param name="headers">The headers to copy.</param>
        /// <returns>A copy of the headers.</returns>
        public static Dictionary<string, IEnumerable<string>> ToDictionary(this HttpContentHeaders headers)
        {
            var headersCopy = new Dictionary<string, IEnumerable<string>>();
            foreach (var h in headers)
            {
                headersCopy.Add(h.Key, h.Value);
            }
            return headersCopy;
        }
    }
}