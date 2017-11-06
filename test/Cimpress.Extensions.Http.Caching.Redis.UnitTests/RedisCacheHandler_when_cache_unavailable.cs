using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;

namespace Cimpress.Extensions.Http.Caching.Redis.UnitTests
{
    public class RedisCacheHandler_when_cache_unavailable
    {
        [Fact]
        public void Gets_the_underlying_data_on_get_error()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
            cache.Setup(c => c.GetAsync(HttpMethod.Get + "http://unittest/", default(CancellationToken))).ThrowsAsync(new Exception("unittest"));
            cache.Setup(c => c.SetAsync(HttpMethod.Get + "http://unittest/", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default(CancellationToken))).Returns(Task.FromResult(true));
            var client = new HttpClient(new RedisCacheHandler(testMessageHandler, new Dictionary<HttpStatusCode, TimeSpan>(), cache.Object));

            // execute
            Func<Task<HttpResponseMessage>> func = async () => await client.GetAsync("http://unittest");
            func.ShouldNotThrow();

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(1);
            cache.Verify(c => c.GetAsync(HttpMethod.Get + "http://unittest/", default(CancellationToken)), Times.Once);
            cache.Verify(c => c.SetAsync(HttpMethod.Get + "http://unittest/", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default(CancellationToken)), Times.Once);
        }

        [Fact]
        public void Ignores_set_cache_exceptions()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
            cache.Setup(c => c.GetAsync(HttpMethod.Get + "http://unittest/", default(CancellationToken))).ReturnsAsync(default(byte[]));
            cache.Setup(c => c.SetAsync(HttpMethod.Get + "http://unittest/", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default(CancellationToken))).Throws<Exception>();
            var client = new HttpClient(new RedisCacheHandler(testMessageHandler, new Dictionary<HttpStatusCode, TimeSpan>(), cache.Object));

            // execute twice
            Func<Task<HttpResponseMessage>> func = async () => await client.GetAsync("http://unittest");
            func.ShouldNotThrow();

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(1);
            cache.Verify(c => c.GetAsync(HttpMethod.Get + "http://unittest/", default(CancellationToken)), Times.Once);
            cache.Verify(c => c.SetAsync(HttpMethod.Get + "http://unittest/", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), default(CancellationToken)), Times.Once);
        }
    }
}