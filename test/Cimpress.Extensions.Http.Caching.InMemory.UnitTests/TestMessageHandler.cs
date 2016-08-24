using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Cimpress.Extensions.Http.Caching.InMemory.UnitTests
{
    internal class TestMessageHandler : HttpMessageHandler
    {
        internal const HttpStatusCode DefaultResponseStatusCode = HttpStatusCode.OK;
        internal const string DefaultContent = "unit test";

        private readonly HttpStatusCode responseStatusCode;
        private readonly string content;

        public int NumberOfCalls { get; set; }
        
        public TestMessageHandler(HttpStatusCode responseStatusCode = DefaultResponseStatusCode, string content = DefaultContent)
        {
            this.responseStatusCode = responseStatusCode;
            this.content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            NumberOfCalls++;

            // simulate actual result
            var response = new HttpResponseMessage
            {
                Content = new StringContent(content),
                StatusCode = responseStatusCode
            };

            return Task.FromResult(response);
        }
    }
}