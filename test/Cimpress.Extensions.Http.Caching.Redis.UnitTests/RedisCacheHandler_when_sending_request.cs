using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Xunit;
using Cimpress.Extensions.Http.Caching.Abstractions;

namespace Cimpress.Extensions.Http.Caching.Redis.UnitTests
{
    public class RedisCacheHandler_when_sending_request
    {
        public static IEnumerable<object[]> GetHeadData
        {
            get
            {
                yield return new object[] {HttpMethod.Get};
                yield return new object[] {HttpMethod.Head};
            }
        }

        [Theory, MemberData(nameof(GetHeadData))]
        public async Task Caches_the_result(HttpMethod method)
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
            cache.Setup(c => c.GetAsync(method + "http://unittest/", It.IsAny<CancellationToken>())).ReturnsAsync(default(byte[]));
            cache.Setup(c => c.SetAsync(method + "http://unittest/", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var client = new HttpClient(new RedisCacheHandler(testMessageHandler, new Dictionary<HttpStatusCode, TimeSpan>(), cache.Object));

            // execute twice
            await client.SendAsync(new HttpRequestMessage(method, "http://unittest"));
            cache.Setup(c => c.GetAsync(method + "http://unittest/", It.IsAny<CancellationToken>())).ReturnsAsync(() => new CacheData(new byte[0], new HttpResponseMessage(HttpStatusCode.OK), null, null).Serialize());
            await client.SendAsync(new HttpRequestMessage(method, "http://unittest"));

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(1);
            cache.Verify(c => c.GetAsync(method + "http://unittest/", It.IsAny<CancellationToken>()), Times.Exactly(2));
            cache.Verify(c => c.SetAsync(method + "http://unittest/", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, MemberData(nameof(GetHeadData))]
        public async Task Gets_the_data_again_after_entry_is_gone_from_cache(HttpMethod method)
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
            cache.Setup(c => c.GetAsync("http://unittest/", It.IsAny<CancellationToken>())).ReturnsAsync(default(byte[]));
            cache.Setup(c => c.SetAsync("http://unittest/", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var client = new HttpClient(new RedisCacheHandler(testMessageHandler, new Dictionary<HttpStatusCode, TimeSpan>(), cache.Object));

            // execute twice
            await client.SendAsync(new HttpRequestMessage(method, "http://unittest"));
            await client.SendAsync(new HttpRequestMessage(method, "http://unittest"));
            
            // validate
            testMessageHandler.NumberOfCalls.Should().Be(2);
            cache.Verify(c => c.GetAsync(method + "http://unittest/", It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Theory, MemberData(nameof(GetHeadData))]
        public async Task Caches_per_url_and_method(HttpMethod method)
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
            cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(default(byte[]));
            cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            var client = new HttpClient(new RedisCacheHandler(testMessageHandler, new Dictionary<HttpStatusCode, TimeSpan>(), cache.Object));

            // execute for different URLs
            await client.SendAsync(new HttpRequestMessage(method, "http://unittest1"));
            await client.SendAsync(new HttpRequestMessage(method, "http://unittest2"));

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(2);
            cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
        
        [Fact]
        public async Task Only_caches_get_and_head_results()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var client = new HttpClient(new RedisCacheHandler(testMessageHandler, new Dictionary<HttpStatusCode, TimeSpan>(), (IDistributedCache) null));

            // execute twice for different methods
            await client.PostAsync("http://unittest", new StringContent(string.Empty));
            await client.PostAsync("http://unittest", new StringContent(string.Empty));
            await client.PutAsync("http://unittest", new StringContent(string.Empty));
            await client.PutAsync("http://unittest", new StringContent(string.Empty));
            await client.DeleteAsync("http://unittest");
            await client.DeleteAsync("http://unittest");

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(6);
        }
        
        [Fact]
        public async Task Data_from_call_matches_data_from_cache()
        {
            // setup
            var expectedContent = "test content";
            var expectedContentTypeHeader = "application/json";
            var expectedEncoding = "utf-8";
            var expectedEtag = new EntityTagHeaderValue("\"unit-test\"");
            var testMessageHandler = new TestMessageHandler(System.Net.HttpStatusCode.OK, expectedContent, expectedContentTypeHeader, expectedEtag);
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
            byte[] savedData = null;
            cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(savedData);
            cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>())).Callback((string key, byte[] data, DistributedCacheEntryOptions o, CancellationToken token) => savedData = data)
                .Returns(Task.FromResult(true));
            var client = new HttpClient(new RedisCacheHandler(testMessageHandler, new Dictionary<HttpStatusCode, TimeSpan>(), cache.Object));

            // execute twice
            var originalResult = await client.GetAsync("http://unittest");
            var cachedResult = await client.GetAsync("http://unittest");
            var originalResultString = await originalResult.Content.ReadAsStringAsync();
            var cachedResultString = await cachedResult.Content.ReadAsStringAsync();

            // validate
            originalResultString.ShouldBeEquivalentTo(cachedResultString);
            originalResultString.ShouldBeEquivalentTo(expectedContent);
            originalResult.Headers.ETag.ShouldBeEquivalentTo(expectedEtag);
            cachedResult.Headers.ETag.ShouldBeEquivalentTo(expectedEtag);
            originalResult.Content.Headers.ContentType.MediaType.Should().Be(expectedContentTypeHeader);
            originalResult.Content.Headers.ContentType.CharSet.Should().Be(expectedEncoding);
            cachedResult.Content.Headers.ContentType.MediaType.Should().Be(expectedContentTypeHeader);
            cachedResult.Content.Headers.ContentType.CharSet.Should().Be(expectedEncoding);
        }

        [Fact]
        public async Task Disable_cache_per_statusCode()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);

            cache.Setup(c => c.GetAsync("http://unittest/", It.IsAny<CancellationToken>())).ReturnsAsync(default(byte[]));
            cache.Setup(c => c.SetAsync("http://unittest/", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));

            var cacheExpirationPerStatusCode = new Dictionary<HttpStatusCode, TimeSpan> {{(HttpStatusCode) 200, TimeSpan.FromSeconds(0)}};


            var client = new HttpClient(new RedisCacheHandler(testMessageHandler, cacheExpirationPerStatusCode, cache.Object));

            // execute twice
            await client.GetAsync("http://unittest");
            await client.GetAsync("http://unittest");

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(2);
            cache.Verify(c => c.SetAsync("http://unittest/", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Exactly(0));
        }
        
        [Fact]
        public async Task Invalidates_cache_correctly()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
            var url = "http://unittest/";
            var key = HttpMethod.Get + url;
            var cacheResult = new CacheData(new byte[0], new HttpResponseMessage(HttpStatusCode.OK), null, null).Serialize();
            var nonCacheResult = default(byte[]);
            var currentResult = nonCacheResult;
            cache.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>())).ReturnsAsync(currentResult);
            cache.Setup(c => c.RemoveAsync(key, It.IsAny<CancellationToken>())).Returns(Task.FromResult(true)).Callback(() => currentResult = nonCacheResult);
            cache.Setup(c => c.SetAsync(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(true))
                .Callback(() => currentResult = cacheResult);

            var cacheExpirationPerStatusCode = new Dictionary<HttpStatusCode, TimeSpan> {{(HttpStatusCode) 200, TimeSpan.FromHours(1)}};

            var handler = new RedisCacheHandler(testMessageHandler, cacheExpirationPerStatusCode, cache.Object);
            var client = new HttpClient(handler);

            // execute twice, with cache invalidation in between
            var uri = new Uri(url);
            await client.GetAsync(uri);
            await handler.InvalidateCache(uri, HttpMethod.Get);
            await client.GetAsync(uri);

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(2);
        }

        [Fact]
        public void Invalidates_cache_per_method()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
            var url = "http://unittest/";
            var getKey = HttpMethod.Get + url;
            var headKey = HttpMethod.Head + url;
            cache.Setup(c => c.RemoveAsync(headKey, default(CancellationToken))).Returns(Task.FromResult(true));
            cache.Setup(c => c.RemoveAsync(getKey, default(CancellationToken))).Throws<Exception>();

            var cacheExpirationPerStatusCode = new Dictionary<HttpStatusCode, TimeSpan> { { (HttpStatusCode)200, TimeSpan.FromHours(1) } };
            var handler = new RedisCacheHandler(testMessageHandler, cacheExpirationPerStatusCode, cache.Object);

            // execute
            Func<Task> func = async () => await handler.InvalidateCache(new Uri(url), HttpMethod.Head);

            // validate
            func.ShouldNotThrow();
            cache.Verify(c => c.RemoveAsync(headKey, default(CancellationToken)), Times.Once);
        }
    }
}