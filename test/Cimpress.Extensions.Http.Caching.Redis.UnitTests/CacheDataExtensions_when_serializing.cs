using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Cimpress.Extensions.Http.Caching.Abstractions;
using FluentAssertions;
using Xunit;

namespace Cimpress.Extensions.Http.Caching.Redis.UnitTests
{
    public class CacheDataExtensions_when_serializing
    {
        [Fact]
        public void Keeps_valid_data_on_serializing_deserializing_roundtrip()
        {
            var content = new byte[] {1, 2, 3, 4, 5};
            var response = new HttpResponseMessage
            {
                Headers = {Age = TimeSpan.FromSeconds(5), ETag = new EntityTagHeaderValue("\"unit-test\""), Location = new Uri("http://unittest")},
                StatusCode = HttpStatusCode.OK,
                ReasonPhrase = "unit-test-reason-phrase",
                Version = new Version(1, 1)
            };
            var expectedData = new SerializableCacheData(content, response);

            byte[] serializedData = expectedData.Serialize();
            SerializableCacheData cachedData = serializedData.Deserialize();

            HttpResponseMessage expectedResponse = expectedData.CachableResponse;
            HttpResponseMessage cacheResponse = cachedData.CachableResponse;
            cachedData.Data.Should().BeEquivalentTo(expectedData.Data);
            cacheResponse.Headers.Age.Should().Be(expectedResponse.Headers.Age);
            cacheResponse.Headers.ETag.Should().Be(expectedResponse.Headers.ETag);
            cacheResponse.Headers.Location.Should().Be(expectedResponse.Headers.Location);
            cacheResponse.StatusCode.Should().Be(expectedResponse.StatusCode);
            cacheResponse.ReasonPhrase.Should().Be(expectedResponse.ReasonPhrase);
            cacheResponse.Version.Should().Be(expectedResponse.Version);
        }
    }
}
