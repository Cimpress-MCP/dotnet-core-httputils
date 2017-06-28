using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Cimpress.Extensions.Http.Caching.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;

namespace Cimpress.Extensions.Http.Caching.Redis.UnitTests
{
    public class RedisCacheFallbackHandler_when_sending_requests
    {
        private string url = "http://unittest/";
        public static IEnumerable<object[]> GetHeadData
        {
            get
            {
                yield return new object[] { HttpMethod.Get };
                yield return new object[] { HttpMethod.Head };
            }
        }

        [Theory, MemberData(nameof(GetHeadData))]
        public async Task Always_calls_the_http_handler(HttpMethod method)
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
            cache.Setup(c => c.SetAsync(method + url, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>())).Returns(Task.FromResult(true));
            var client = new HttpClient(new RedisCacheFallbackHandler(testMessageHandler, TimeSpan.FromDays(1), TimeSpan.FromDays(1), cache.Object));

            // execute twice
            await client.SendAsync(new HttpRequestMessage(method, url));
            cache.Verify(c => c.SetAsync(method + url, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Once); // ensure it's cached before the 2nd call
            await client.SendAsync(new HttpRequestMessage(method, url));

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(2);
        }

        [Theory, MemberData(nameof(GetHeadData))]
        public async Task Always_updates_the_cache_on_success(HttpMethod method)
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
            var cacheTime = TimeSpan.FromSeconds(123);
            cache.Setup(c => c.SetAsync(method + url, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>())).Returns(Task.FromResult(true));
            var client = new HttpClient(new RedisCacheFallbackHandler(testMessageHandler, TimeSpan.FromDays(1), cacheTime, cache.Object));

            // execute twice, validate cache is called each time
            await client.SendAsync(new HttpRequestMessage(method, url));
            cache.Verify(c => c.SetAsync(method + url, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Once);
            await client.SendAsync(new HttpRequestMessage(method, url));
            cache.Verify(c => c.SetAsync(method + url, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Exactly(2));
        }

        [Theory, MemberData(nameof(GetHeadData))]
        public async Task Never_updates_the_cache_on_failure(HttpMethod method)
        {
            // setup
            var testMessageHandler = new TestMessageHandler(HttpStatusCode.InternalServerError);
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
            var cacheTime = TimeSpan.FromSeconds(123);
            cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>())).Returns(Task.FromResult(true));
            cache.Setup(c => c.GetAsync(method + url)).ReturnsAsync(default(byte[]));
            var client = new HttpClient(new RedisCacheFallbackHandler(testMessageHandler, TimeSpan.FromDays(1), cacheTime, cache.Object));

            // execute
            await client.SendAsync(new HttpRequestMessage(method, url));

            // validate
            cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Never);
        }

        [Theory, MemberData(nameof(GetHeadData))]
        public async Task Tries_to_access_cache_on_failure_but_returns_error_if_not_in_cache(HttpMethod method)
        {
            // setup
            var testMessageHandler = new TestMessageHandler(HttpStatusCode.InternalServerError);
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
            var cacheTime = TimeSpan.FromSeconds(123);
            cache.Setup(c => c.GetAsync(method + url)).ReturnsAsync(default(byte[]));
            var client = new HttpClient(new RedisCacheFallbackHandler(testMessageHandler, TimeSpan.FromDays(1), cacheTime, cache.Object));

            // execute
            var result = await client.SendAsync(new HttpRequestMessage(method, url));

            // validate
            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

        [Theory, MemberData(nameof(GetHeadData))]
        public async Task Gets_it_from_the_http_call_after_being_in_cache(HttpMethod method)
        {
            // setup
            var testMessageHandler1 = new TestMessageHandler(content: "message-1", delay: TimeSpan.FromMilliseconds(100));
            var testMessageHandler2 = new TestMessageHandler(content: "message-2");
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
            cache.Setup(c => c.SetAsync(method + url, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>())).Returns(Task.FromResult(true));
            var client1 = new HttpClient(new RedisCacheFallbackHandler(testMessageHandler1, TimeSpan.FromMilliseconds(1), TimeSpan.FromDays(1), cache.Object));
            var client2 = new HttpClient(new RedisCacheFallbackHandler(testMessageHandler2, TimeSpan.FromMilliseconds(1), TimeSpan.FromDays(1), cache.Object));

            // execute twice
            var result1 = await client1.SendAsync(new HttpRequestMessage(method, url));
            cache.Verify(c => c.SetAsync(method + url, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Once);
            var result2 = await client2.SendAsync(new HttpRequestMessage(method, url));

            // validate
            // - that each message handler got called
            testMessageHandler1.NumberOfCalls.Should().Be(1);
            testMessageHandler2.NumberOfCalls.Should().Be(1);

            // - that the 2nd result got served from cache
            var data1 = await result1.Content.ReadAsStringAsync();
            var data2 = await result2.Content.ReadAsStringAsync();
            data1.Should().BeEquivalentTo("message-1");
            data2.Should().BeEquivalentTo("message-2");
        }

        [Theory, MemberData(nameof(GetHeadData))]
        public async Task Gets_it_from_the_cache_when_unsuccessful(HttpMethod method)
        {
            // setup
            var responseToSerialize = await new HttpClient(new TestMessageHandler(HttpStatusCode.OK, "message-1")).GetAsync(url);
            var cacheEntryToSerialize = await responseToSerialize.ToCacheEntry();
            var serializedCacheEntry = cacheEntryToSerialize.Serialize();

            var testMessageHandler1 = new TestMessageHandler(HttpStatusCode.OK, "message-1");
            var testMessageHandler2 = new TestMessageHandler(HttpStatusCode.InternalServerError, "message-2");
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
            cache.SetupSequence(c => c.GetAsync(method + url)).ReturnsAsync(default(byte[])).ReturnsAsync(serializedCacheEntry);
            var client1 = new HttpClient(new RedisCacheFallbackHandler(testMessageHandler1, TimeSpan.FromDays(1), TimeSpan.FromDays(1), cache.Object));
            var client2 = new HttpClient(new RedisCacheFallbackHandler(testMessageHandler2, TimeSpan.FromDays(1), TimeSpan.FromDays(1), cache.Object));

            // execute twice
            var result1 = await client1.SendAsync(new HttpRequestMessage(method, url));
            var result2 = await client2.SendAsync(new HttpRequestMessage(method, url));

            // validate
            // - that each message handler got called
            testMessageHandler1.NumberOfCalls.Should().Be(1);
            testMessageHandler2.NumberOfCalls.Should().Be(1);

            // - that the 2nd result got served from cache
            var data1 = await result1.Content.ReadAsStringAsync();
            var data2 = await result2.Content.ReadAsStringAsync();
            data1.Should().BeEquivalentTo("message-1");
            data2.Should().BeEquivalentTo(data1);
        }
    }
}
