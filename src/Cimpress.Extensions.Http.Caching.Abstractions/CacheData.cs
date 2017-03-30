using System.Collections.Generic;
using System.Net.Http;

namespace Cimpress.Extensions.Http.Caching.Abstractions
{
    /// <summary>
    /// The data object that is used to put into the cache.
    /// </summary>
    public class CacheData
    {
        public CacheData(byte[] data, HttpResponseMessage cachableResponse, Dictionary<string, IEnumerable<string>> contentHeaders = null)
        {
            Data = data;
            CachableResponse = cachableResponse;
            ContentHeaders = contentHeaders;
        }

        /// <summary>
        /// The cachable part of a previously retrieved response (excludes the content and request).
        /// </summary>
        public HttpResponseMessage CachableResponse { get; set; }

        /// <summary>
        /// The content of the response.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// The content headers of the response
        /// </summary>
        public Dictionary<string, IEnumerable<string>> ContentHeaders { get; set; }
    }
}