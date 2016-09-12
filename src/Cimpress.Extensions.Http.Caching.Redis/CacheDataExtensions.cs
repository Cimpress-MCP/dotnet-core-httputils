using System;
using Newtonsoft.Json;

namespace Cimpress.Extensions.Http.Caching.Redis
{
    public static class CacheDataExtensions
    {
        public static byte[] Serialize(this SerializableCacheData cacheData)
        {
            string json = JsonConvert.SerializeObject(cacheData);
            byte[] bytes = new byte[json.Length * sizeof(char)];
            Buffer.BlockCopy(json.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static SerializableCacheData Deserialize(this byte[] cacheData)
        {
            char[] chars = new char[cacheData.Length / sizeof(char)];
            Buffer.BlockCopy(cacheData, 0, chars, 0, cacheData.Length);
            string json = new string(chars);
            SerializableCacheData data = JsonConvert.DeserializeObject<SerializableCacheData>(json);

            var headers = data.CachableResponse.Headers;
            foreach (var header in data.ResponseHeaders)
            {
                headers.Add(header.Key, header.Value);
            }

            return data;
        }
    }
}