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

        public static IEnumerable<object[]> SerializedDataTestCaseData
        {
            get
            {
                Func<string, byte[]> convertToBinary = base64String => Convert.FromBase64String(base64String);

                yield return new object[]
                {
                    "Add Content Headers",
                    new HttpResponseMessage
                    {
                        Headers = {Age = TimeSpan.FromSeconds(5), ETag = new EntityTagHeaderValue("\"unit-test\""), Location = new Uri("http://unittest")},
                        StatusCode = HttpStatusCode.OK, ReasonPhrase = "unit-test-reason-phrase", Version = new Version(1, 1)
                    },
                    new byte[] {1, 2, 3, 4, 5},
                    new Dictionary<string, IEnumerable<string>>
                    {
                        {"Content-Type", new[] {"application/json"}}
                    },
                    convertToBinary("ewAiAEMAYQBjAGgAYQBiAGwAZQBSAGUAcwBwAG8AbgBzAGUAIgA6AHsAIgBWAGUAcgBzAGkAbwBuACIAOgB7ACIATQBhAGoAbwByACIAOgAxACwAIgBNAGkAbgBvAHIAIgA6ADEALAAiAEIAdQBpAGwAZAAiADoALQAxACwAIgBSAGUAdgBpAHMAaQBvAG4AIgA6AC0AMQAsACIATQBhAGoAbwByAFIAZQB2AGkAcwBpAG8AbgAiADoALQAxACwAIgBNAGkAbgBvAHIAUgBlAHYAaQBzAGkAbwBuACIAOgAtADEAfQAsACIAQwBvAG4AdABlAG4AdAAiADoAbgB1AGwAbAAsACIAUwB0AGEAdAB1AHMAQwBvAGQAZQAiADoAMgAwADAALAAiAFIAZQBhAHMAbwBuAFAAaAByAGEAcwBlACIAOgAiAHUAbgBpAHQALQB0AGUAcwB0AC0AcgBlAGEAcwBvAG4ALQBwAGgAcgBhAHMAZQAiACwAIgBIAGUAYQBkAGUAcgBzACIAOgBbAHsAIgBLAGUAeQAiADoAIgBBAGcAZQAiACwAIgBWAGEAbAB1AGUAIgA6AFsAIgA1ACIAXQB9ACwAewAiAEsAZQB5ACIAOgAiAEUAVABhAGcAIgAsACIAVgBhAGwAdQBlACIAOgBbACIAXAAiAHUAbgBpAHQALQB0AGUAcwB0AFwAIgAiAF0AfQAsAHsAIgBLAGUAeQAiADoAIgBMAG8AYwBhAHQAaQBvAG4AIgAsACIAVgBhAGwAdQBlACIAOgBbACIAaAB0AHQAcAA6AC8ALwB1AG4AaQB0AHQAZQBzAHQALwAiAF0AfQBdACwAIgBSAGUAcQB1AGUAcwB0AE0AZQBzAHMAYQBnAGUAIgA6AG4AdQBsAGwALAAiAEkAcwBTAHUAYwBjAGUAcwBzAFMAdABhAHQAdQBzAEMAbwBkAGUAIgA6AHQAcgB1AGUAfQAsACIARABhAHQAYQAiADoAIgBBAFEASQBEAEIAQQBVAD0AIgAsACIASABlAGEAZABlAHIAcwAiADoAewAiAEEAZwBlACIAOgBbACIANQAiAF0ALAAiAEUAVABhAGcAIgA6AFsAIgBcACIAdQBuAGkAdAAtAHQAZQBzAHQAXAAiACIAXQAsACIATABvAGMAYQB0AGkAbwBuACIAOgBbACIAaAB0AHQAcAA6AC8ALwB1AG4AaQB0AHQAZQBzAHQALwAiAF0AfQAsACIAQwBvAG4AdABlAG4AdABIAGUAYQBkAGUAcgBzACIAOgB7ACIAQwBvAG4AdABlAG4AdAAtAFQAeQBwAGUAIgA6AFsAIgBhAHAAcABsAGkAYwBhAHQAaQBvAG4ALwBqAHMAbwBuACIAXQB9AH0A")
                };

                yield return new object[]
                {
                    "Baseline",
                    new HttpResponseMessage
                    {
                        Headers = {Age = TimeSpan.FromSeconds(5), ETag = new EntityTagHeaderValue("\"unit-test\""), Location = new Uri("http://unittest")},
                        StatusCode = HttpStatusCode.OK, ReasonPhrase = "unit-test-reason-phrase", Version = new Version(1, 1)
                    },
                    new byte[] {1, 2, 3, 4, 5},
                    null,
                    convertToBinary("ewAiAEMAYQBjAGgAYQBiAGwAZQBSAGUAcwBwAG8AbgBzAGUAIgA6AHsAIgBWAGUAcgBzAGkAbwBuACIAOgB7ACIATQBhAGoAbwByACIAOgAxACwAIgBNAGkAbgBvAHIAIgA6ADEALAAiAEIAdQBpAGwAZAAiADoALQAxACwAIgBSAGUAdgBpAHMAaQBvAG4AIgA6AC0AMQAsACIATQBhAGoAbwByAFIAZQB2AGkAcwBpAG8AbgAiADoALQAxACwAIgBNAGkAbgBvAHIAUgBlAHYAaQBzAGkAbwBuACIAOgAtADEAfQAsACIAQwBvAG4AdABlAG4AdAAiADoAbgB1AGwAbAAsACIAUwB0AGEAdAB1AHMAQwBvAGQAZQAiADoAMgAwADAALAAiAFIAZQBhAHMAbwBuAFAAaAByAGEAcwBlACIAOgAiAHUAbgBpAHQALQB0AGUAcwB0AC0AcgBlAGEAcwBvAG4ALQBwAGgAcgBhAHMAZQAiACwAIgBIAGUAYQBkAGUAcgBzACIAOgBbAHsAIgBLAGUAeQAiADoAIgBBAGcAZQAiACwAIgBWAGEAbAB1AGUAIgA6AFsAIgA1ACIAXQB9ACwAewAiAEsAZQB5ACIAOgAiAEUAVABhAGcAIgAsACIAVgBhAGwAdQBlACIAOgBbACIAXAAiAHUAbgBpAHQALQB0AGUAcwB0AFwAIgAiAF0AfQAsAHsAIgBLAGUAeQAiADoAIgBMAG8AYwBhAHQAaQBvAG4AIgAsACIAVgBhAGwAdQBlACIAOgBbACIAaAB0AHQAcAA6AC8ALwB1AG4AaQB0AHQAZQBzAHQALwAiAF0AfQBdACwAIgBSAGUAcQB1AGUAcwB0AE0AZQBzAHMAYQBnAGUAIgA6AG4AdQBsAGwALAAiAEkAcwBTAHUAYwBjAGUAcwBzAFMAdABhAHQAdQBzAEMAbwBkAGUAIgA6AHQAcgB1AGUAfQAsACIARABhAHQAYQAiADoAIgBBAFEASQBEAEIAQQBVAD0AIgAsACIASABlAGEAZABlAHIAcwAiADoAewAiAEEAZwBlACIAOgBbACIANQAiAF0ALAAiAEUAVABhAGcAIgA6AFsAIgBcACIAdQBuAGkAdAAtAHQAZQBzAHQAXAAiACIAXQAsACIATABvAGMAYQB0AGkAbwBuACIAOgBbACIAaAB0AHQAcAA6AC8ALwB1AG4AaQB0AHQAZQBzAHQALwAiAF0AfQB9AA==")
                };

                yield return new object[]
                {
                    "Without either header",
                    new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK, ReasonPhrase = "unit-test-reason-phrase", Version = new Version(1, 1)
                    },
                    new byte[] {1, 2, 3, 4, 5},
                    null,
                    convertToBinary("ewAiAEMAYQBjAGgAYQBiAGwAZQBSAGUAcwBwAG8AbgBzAGUAIgA6AHsAIgBWAGUAcgBzAGkAbwBuACIAOgB7ACIATQBhAGoAbwByACIAOgAxACwAIgBNAGkAbgBvAHIAIgA6ADEALAAiAEIAdQBpAGwAZAAiADoALQAxACwAIgBSAGUAdgBpAHMAaQBvAG4AIgA6AC0AMQAsACIATQBhAGoAbwByAFIAZQB2AGkAcwBpAG8AbgAiADoALQAxACwAIgBNAGkAbgBvAHIAUgBlAHYAaQBzAGkAbwBuACIAOgAtADEAfQAsACIAQwBvAG4AdABlAG4AdAAiADoAbgB1AGwAbAAsACIAUwB0AGEAdAB1AHMAQwBvAGQAZQAiADoAMgAwADAALAAiAFIAZQBhAHMAbwBuAFAAaAByAGEAcwBlACIAOgAiAHUAbgBpAHQALQB0AGUAcwB0AC0AcgBlAGEAcwBvAG4ALQBwAGgAcgBhAHMAZQAiACwAIgBIAGUAYQBkAGUAcgBzACIAOgBbAHsAIgBLAGUAeQAiADoAIgBBAGcAZQAiACwAIgBWAGEAbAB1AGUAIgA6AFsAIgA1ACIAXQB9ACwAewAiAEsAZQB5ACIAOgAiAEUAVABhAGcAIgAsACIAVgBhAGwAdQBlACIAOgBbACIAXAAiAHUAbgBpAHQALQB0AGUAcwB0AFwAIgAiAF0AfQAsAHsAIgBLAGUAeQAiADoAIgBMAG8AYwBhAHQAaQBvAG4AIgAsACIAVgBhAGwAdQBlACIAOgBbACIAaAB0AHQAcAA6AC8ALwB1AG4AaQB0AHQAZQBzAHQALwAiAF0AfQBdACwAIgBSAGUAcQB1AGUAcwB0AE0AZQBzAHMAYQBnAGUAIgA6AG4AdQBsAGwALAAiAEkAcwBTAHUAYwBjAGUAcwBzAFMAdABhAHQAdQBzAEMAbwBkAGUAIgA6AHQAcgB1AGUAfQAsACIARABhAHQAYQAiADoAIgBBAFEASQBEAEIAQQBVAD0AIgB9AA==")
                };

                yield return new object[]
                {
                    "Has extra data",
                    new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK, ReasonPhrase = "unit-test-reason-phrase", Version = new Version(1, 1)
                    },
                    new byte[] {1, 2, 3, 4, 5},
                    null,
                    convertToBinary("ewAiAEMAYQBjAGgAYQBiAGwAZQBSAGUAcwBwAG8AbgBzAGUAIgA6AHsAIgBWAGUAcgBzAGkAbwBuACIAOgB7ACIATQBhAGoAbwByACIAOgAxACwAIgBNAGkAbgBvAHIAIgA6ADEALAAiAEIAdQBpAGwAZAAiADoALQAxACwAIgBSAGUAdgBpAHMAaQBvAG4AIgA6AC0AMQAsACIATQBhAGoAbwByAFIAZQB2AGkAcwBpAG8AbgAiADoALQAxACwAIgBNAGkAbgBvAHIAUgBlAHYAaQBzAGkAbwBuACIAOgAtADEAfQAsACIAQwBvAG4AdABlAG4AdAAiADoAbgB1AGwAbAAsACIAUwB0AGEAdAB1AHMAQwBvAGQAZQAiADoAMgAwADAALAAiAFIAZQBhAHMAbwBuAFAAaAByAGEAcwBlACIAOgAiAHUAbgBpAHQALQB0AGUAcwB0AC0AcgBlAGEAcwBvAG4ALQBwAGgAcgBhAHMAZQAiACwAIgBIAGUAYQBkAGUAcgBzACIAOgBbAHsAIgBLAGUAeQAiADoAIgBBAGcAZQAiACwAIgBWAGEAbAB1AGUAIgA6AFsAIgA1ACIAXQB9ACwAewAiAEsAZQB5ACIAOgAiAEUAVABhAGcAIgAsACIAVgBhAGwAdQBlACIAOgBbACIAXAAiAHUAbgBpAHQALQB0AGUAcwB0AFwAIgAiAF0AfQAsAHsAIgBLAGUAeQAiADoAIgBMAG8AYwBhAHQAaQBvAG4AIgAsACIAVgBhAGwAdQBlACIAOgBbACIAaAB0AHQAcAA6AC8ALwB1AG4AaQB0AHQAZQBzAHQALwAiAF0AfQBdACwAIgBSAGUAcQB1AGUAcwB0AE0AZQBzAHMAYQBnAGUAIgA6AG4AdQBsAGwALAAiAEkAcwBTAHUAYwBjAGUAcwBzAFMAdABhAHQAdQBzAEMAbwBkAGUAIgA6AHQAcgB1AGUAfQAsACIARABhAHQAYQAiADoAIgBBAFEASQBEAEIAQQBVAD0AIgAsACIATgBFAFcARgBJAEUATABEACIAOgB0AHIAdQBlAH0A")
                };
            }
        }

        [Theory, MemberData(nameof(SerializedDataTestCaseData))]
        public void Can_deserialize_previous_versions(string testName, HttpResponseMessage expectedMessage, byte[] expectedData, Dictionary<string, IEnumerable<string>> expectedContentHeaders, byte[] serializedData)
        {
            //Serialized the saved data
            var cachedData = serializedData.Deserialize();

            //Verify it matches
            cachedData.Data.Should().BeEquivalentTo(expectedData);

            var headers = expectedMessage.Headers?.Where(h => h.Value != null && h.Value.Any()).ToDictionary(h => h.Key, h => h.Value);
            if (headers.Count > 0)
            {
                cachedData.Headers["Age"].ShouldAllBeEquivalentTo(headers["Age"]);
                cachedData.Headers["ETag"].ShouldAllBeEquivalentTo(headers["ETag"]);
                cachedData.Headers["Location"].ShouldAllBeEquivalentTo(headers["Location"]);
            }
            if (expectedContentHeaders != null)
            {
                cachedData.ContentHeaders["Content-Type"].ShouldAllBeEquivalentTo(expectedContentHeaders["Content-Type"]);
            }
            cachedData.CachableResponse.StatusCode.Should().Be(expectedMessage.StatusCode);
            cachedData.CachableResponse.ReasonPhrase.Should().Be(expectedMessage.ReasonPhrase);
            cachedData.CachableResponse.Version.Should().Be(expectedMessage.Version);
        }

        [Fact]
        public void Create_new_serialized_data_for_test()
        {
            var response = new HttpResponseMessage
            {
                Headers = {Age = TimeSpan.FromSeconds(5), ETag = new EntityTagHeaderValue("\"unit-test\""), Location = new Uri("http://unittest")},
                StatusCode = HttpStatusCode.OK,
                ReasonPhrase = "unit-test-reason-phrase",
                Version = new Version(1, 1)
            };
            var headers = response.Headers.Where(h => h.Value != null && h.Value.Any()).ToDictionary(h => h.Key, h => h.Value);
            var contentHeaders = new Dictionary<string, IEnumerable<string>>
            {
                {"Content-Type", new[] {"application/json"}}
            };

            var expectedData = new CacheData(new byte[] {1, 2, 3, 4, 5}, response, headers, contentHeaders);
            var newserializedData = expectedData.Serialize();
            var base64Bytes = Convert.ToBase64String(newserializedData);
            //System.Console.WriteLine(base64Bytes);
        }
    }
}
