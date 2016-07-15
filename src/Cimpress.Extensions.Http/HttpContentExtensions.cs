using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Cimpress.Extensions.Http
{
    public static class HttpContentExtensions
    {
        public static JsonSerializerSettings StandardSerializerSettings { get; set; }

        static HttpContentExtensions()
        {
            StandardSerializerSettings = new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore, ContractResolver = new CamelCasePropertyNamesContractResolver() };
        }

        public static async Task<T> ReadAsAsync<T>(this HttpContent content, JsonSerializerSettings serializerSettings = null)
        {
            var data = await content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(data, serializerSettings ?? StandardSerializerSettings);
        }

        public static HttpContent ToHttpContent(this object data, JsonSerializerSettings serializerSettings = null)
        {
            var content = JsonConvert.SerializeObject(data, serializerSettings ?? StandardSerializerSettings);
            var httpContent = new StringContent(content);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return httpContent;
        }
    }
}