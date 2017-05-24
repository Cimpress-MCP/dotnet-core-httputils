using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace Cimpress.Extensions.Http.Caching.Redis.UnitTests
{
    internal class TestMessageHandler : HttpMessageHandler
    {
        internal const HttpStatusCode DefaultResponseStatusCode = HttpStatusCode.OK;
        internal const string DefaultContent = "unit test";
        internal const string DefaultContentType = "text/plain";

        private readonly HttpStatusCode responseStatusCode;
        private readonly string content;
        private readonly string contentType;
        private readonly TimeSpan delay;
        private readonly Encoding encoding;
        private readonly EntityTagHeaderValue etag;

        public int NumberOfCalls { get; set; }
        
        public TestMessageHandler(HttpStatusCode responseStatusCode = DefaultResponseStatusCode, string content = DefaultContent, string contentType = DefaultContentType, EntityTagHeaderValue etag = null, Encoding encoding = null, TimeSpan delay = default(TimeSpan))
        {
            this.responseStatusCode = responseStatusCode;
            this.content = content;
            this.contentType = contentType;
            this.delay = delay;
            this.encoding = encoding ?? Encoding.UTF8;
            this.etag = etag ?? default(EntityTagHeaderValue);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            NumberOfCalls++;

            // optional delay
            if (delay != default(TimeSpan))
            {
                await Task.Delay(delay, cancellationToken);
            }

            // simulate actual result
            return new HttpResponseMessage {Headers = {ETag = this.etag}, Content = new StringContent(content, this.encoding, this.contentType), StatusCode = responseStatusCode};
        }

    }
}