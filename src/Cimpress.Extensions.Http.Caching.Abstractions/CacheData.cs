using System.Net.Http;

namespace Cimpress.Extensions.Http.Caching.Abstractions
{
    /// <summary>
    /// The data object that is used to put into the cache.
    /// </summary>
    public class CacheData
    {
        public CacheData(byte[] data, HttpResponseMessage cachableResponse)
        {
            Data = data;
            CachableResponse = cachableResponse;
        }

        /// <summary>
        /// The cachable part of a previously retrieved response (excludes the content and request).
        /// </summary>
        public HttpResponseMessage CachableResponse { get; set; }

        /// <summary>
        /// The content of the response.
        /// </summary>
        public byte[] Data { get; set; }
    }
}