using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Cimpress.Extensions.Http.Caching.Abstractions;

namespace Cimpress.Extensions.Http.Caching.Redis
{
    public class SerializableCacheData : CacheData
    {
        public SerializableCacheData(byte[] data, HttpResponseMessage cachableResponse) : base(data, cachableResponse)
        {
            ResponseHeaders = cachableResponse.Headers.Where(h => h.Value != null && h.Value.Any()).ToDictionary(h => h.Key, h => h.Value);
        }
        public Dictionary<string, IEnumerable<string>> ResponseHeaders { get; set; }

    }
}