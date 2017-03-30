using System;
using System.Collections.Generic;
using System.Linq;
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
            var expectedSeconds = 5;
            var response = new HttpResponseMessage
            {
                Headers = {Age = TimeSpan.FromSeconds(expectedSeconds), ETag = new EntityTagHeaderValue("\"unit-test\""), Location = new Uri("http://unittest")},
                StatusCode = HttpStatusCode.OK,
                ReasonPhrase = "unit-test-reason-phrase",
                Version = new Version(1, 1)
            };
            var headers = response.Headers.Where(h => h.Value != null && h.Value.Any()).ToDictionary(h => h.Key, h => h.Value);
            var contentHeaders = new Dictionary<string, IEnumerable<string>>
            {
                {"Content-Type", new[] {"application/json"}}
            };

            var expectedData = new CacheData(content, response, headers, contentHeaders);
            var serializedData = expectedData.Serialize();
            var cachedData = serializedData.Deserialize();

            cachedData.Data.Should().BeEquivalentTo(content);
            cachedData.Headers["Age"].First().Should().Be(expectedSeconds.ToString());
            cachedData.Headers["ETag"].First().Should().Be(response.Headers.ETag.ToString());
            cachedData.Headers["Location"].First().Should().Be(response.Headers.Location.ToString());
            cachedData.ContentHeaders["Content-Type"].First().ToString().Should().Be(contentHeaders["Content-Type"].First());
            cachedData.CachableResponse.StatusCode.Should().Be(response.StatusCode);
            cachedData.CachableResponse.ReasonPhrase.Should().Be(response.ReasonPhrase);
            cachedData.CachableResponse.Version.Should().Be(response.Version);
        }
    }
}
