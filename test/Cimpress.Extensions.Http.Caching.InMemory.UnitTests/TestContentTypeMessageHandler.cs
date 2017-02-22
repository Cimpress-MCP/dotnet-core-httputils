using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cimpress.Extensions.Http.Caching.InMemory.UnitTests
{
    internal class TestContentTypeMessageHandler : TestMessageHandler
    {
        private readonly string contentType;
        private readonly Encoding encoding;
       
        public TestContentTypeMessageHandler(string contentType = "text/plain", Encoding encoding = null)
            : base()
        {
            this.contentType = contentType;
            this.encoding = encoding ?? Encoding.UTF8;
        }

        protected override HttpResponseMessage makeResponse()
        {
            return new HttpResponseMessage
            {
                Content = new StringContent(content, this.encoding, this.contentType),
                StatusCode = responseStatusCode
            };
        }
    }
}