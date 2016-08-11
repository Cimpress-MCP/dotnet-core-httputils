using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cimpress.Extensions.Http.UnitTests
{
    public class DataUriConversion_when_downloading_images_and_converting_to_base_64
    {
        private readonly Mock<ILogger> logger;
        private readonly Mock<IFileInfo> fileInfo;

        public DataUriConversion_when_downloading_images_and_converting_to_base_64()
        {
            logger = new Mock<ILogger>(MockBehavior.Loose);
            fileInfo = new Mock<IFileInfo>(MockBehavior.Strict);
        }

        [Fact]
        public void Does_not_throw()
        {
            // execute
            Func<Task> func = async () => await "http://foo".DownloadImageAndConvertToDataUri(logger.Object, fileInfo.Object, "image/unittest", new HttpUtilsTestMessageHandler());

            // verify
            func.ShouldNotThrow();
        }

        [Fact]
        public async Task Uses_data_uri_scheme()
        {
            // execute
            var mediaType = "image/foo";
            var result = await "http://foo".DownloadImageAndConvertToDataUri(logger.Object, fileInfo.Object, "image/unittest", new HttpUtilsTestMessageHandler(HttpStatusCode.OK, mediaType));

            // verify
            result.Should().StartWith($"data:{mediaType};base64,");
        }

        [Fact]
        public async Task Converts_data_to_base64()
        {
            // execute
            var result = await "http://foo".DownloadImageAndConvertToDataUri(logger.Object, fileInfo.Object, "image/unittest", new HttpUtilsTestMessageHandler());

            // verify
            var data = Convert.ToBase64String(Enumerable.Range(0, 100).Select(x => (byte) x).ToArray());
            result.Should().EndWith(data);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("              ")]
        public async Task Gets_default_file_on_empty_url(string url)
        {
            // setup
            fileInfo.Setup(s => s.CreateReadStream()).Returns(new MemoryStream());

            // execute
            await url.DownloadImageAndConvertToDataUri(logger.Object, fileInfo.Object, "image/unittest", new HttpClientHandler());

            // verify
            fileInfo.Verify(s => s.CreateReadStream(), Times.Once);
        }

        [Fact]
        public async Task Gets_default_file_on_exception()
        {
            // setup
            fileInfo.Setup(s => s.CreateReadStream()).Returns(new MemoryStream());

            // execute
            await "foo".DownloadImageAndConvertToDataUri(logger.Object, fileInfo.Object, "image/unittest", new HttpUtilsTestMessageHandler());

            // verify
            fileInfo.Verify(s => s.CreateReadStream(), Times.Once);
        }

        private class HttpUtilsTestMessageHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode responseStatusCode;
            private readonly MediaTypeHeaderValue contentType;

            public HttpUtilsTestMessageHandler(HttpStatusCode responseStatusCode = HttpStatusCode.OK, string mediaType = "image/jpg")
            {
                this.responseStatusCode = responseStatusCode;
                contentType = new MediaTypeHeaderValue(mediaType);
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // simulate actual result
                var ms = new MemoryStream(Enumerable.Range(0, 100).Select(x => (byte) x).ToArray());
                var streamContent = new StreamContent(ms);
                streamContent.Headers.ContentType = contentType;
                var response = new HttpResponseMessage(HttpStatusCode.OK) {Content = streamContent, StatusCode = responseStatusCode};

                return Task.FromResult(response);
            }
        }
    }
}
