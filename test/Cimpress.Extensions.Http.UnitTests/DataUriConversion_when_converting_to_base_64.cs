using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using FluentAssertions;
using Moq;
using Xunit;
using System.Linq;

namespace Cimpress.Extensions.Http.UnitTests
{
    public class DataUriConversion_when_converting_to_base_64
    {
        private const string rawDataToConvert = "unittest";
        private const string rawDataBase64 = "dW5pdHRlc3Q=";

        [Fact]
        public async Task Converts_fileinfo_to_datauri()
        {
            // setup
            var stream = SetupMemoryStream();
            var fileInfo = new Mock<IFileInfo>(MockBehavior.Strict);
            fileInfo.Setup(s => s.CreateReadStream()).Returns(stream);

            // execute
            var result = await fileInfo.Object.ToDataUri("image/unittest");

            // verify
            VerifyResult(result);
        }

        [Fact]
        public async Task Converts_stream_to_datauri()
        {
            // setup
            var stream = SetupMemoryStream();

            // execute
            var result = await stream.ToDataUri("image/unittest");

            // verify
            VerifyResult(result);
        }

        [Fact]
        public void Converts_byte_to_datauri()
        {
            // setup
            byte[] data = rawDataToConvert.ToCharArray().Select(Convert.ToByte).ToArray();

            // execute
            var result = data.ToDataUri("image/unittest");

            // verify
            VerifyResult(result);
        }

        [Fact]
        public void Converts_string_to_datauri()
        {
            // execute
            var result = rawDataBase64.ToDataUri("image/unittest");

            // verify
            VerifyResult(result);
        }

        private static MemoryStream SetupMemoryStream()
        {
            byte[] data = rawDataToConvert.ToCharArray().Select(Convert.ToByte).ToArray();
            var stream = new MemoryStream(data);
            return stream;
        }

        private void VerifyResult(string result)
        {
            var expected = $"data:image/unittest;base64,{rawDataBase64}";
            result.Should().Be(expected);
        }
    }
}
