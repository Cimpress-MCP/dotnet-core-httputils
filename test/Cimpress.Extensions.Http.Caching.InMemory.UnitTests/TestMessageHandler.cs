using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace Cimpress.Extensions.Http.Caching.InMemory.UnitTests
{
    internal class TestMessageHandler : HttpMessageHandler
    {
        internal const HttpStatusCode DefaultResponseStatusCode = HttpStatusCode.OK;
        internal const string DefaultContent = "unit test";
        internal const string DefaultContentType = "text/plain";

        private readonly HttpStatusCode responseStatusCode;
        private readonly string content;
        private readonly string contentType;
        private readonly Encoding encoding;

        public int NumberOfCalls { get; set; }
        
        public TestMessageHandler(HttpStatusCode responseStatusCode = DefaultResponseStatusCode, string content = DefaultContent, string contentType = DefaultContentType, Encoding encoding = null)
        {
            this.responseStatusCode = responseStatusCode;
            this.content = content;
            this.contentType = contentType;
            this.encoding = encoding ?? Encoding.UTF8;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            NumberOfCalls++;

            // simulate actual result
            return Task.FromResult(new HttpResponseMessage()
            {
                Content = new StringContent(content, this.encoding, this.contentType),
                StatusCode = responseStatusCode
            });
        }

    }
}