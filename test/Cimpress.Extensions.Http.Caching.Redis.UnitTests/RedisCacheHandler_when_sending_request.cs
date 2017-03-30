using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        [Fact]
        public async Task Caches_the_result()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
            cache.Setup(c => c.GetAsync("http://unittest/")).ReturnsAsync(null);
            cache.Setup(c => c.SetAsync("http://unittest/", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>())).Returns(Task.FromResult(true));
            var client = new HttpClient(new RedisCacheHandler(testMessageHandler, new Dictionary<HttpStatusCode, TimeSpan>(), cache.Object));

            // execute twice
            await client.GetAsync("http://unittest");
            cache.Setup(c => c.GetAsync("http://unittest/")).ReturnsAsync(new CacheData(new byte[0], new HttpResponseMessage(HttpStatusCode.OK), null, null).Serialize());
            await client.GetAsync("http://unittest");

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(1);
            cache.Verify(c => c.GetAsync("http://unittest/"), Times.Exactly(2));
            cache.Verify(c => c.SetAsync("http://unittest/", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Once);
        }

        [Fact]
        public async Task Gets_the_data_again_after_entry_is_gone_from_cache()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
            cache.Setup(c => c.GetAsync("http://unittest/")).ReturnsAsync(null);
            cache.Setup(c => c.SetAsync("http://unittest/", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>())).Returns(Task.FromResult(true));
            var client = new HttpClient(new RedisCacheHandler(testMessageHandler, new Dictionary<HttpStatusCode, TimeSpan>(), cache.Object));

            // execute twice
            await client.GetAsync("http://unittest");
            await client.GetAsync("http://unittest");

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(2);
            cache.Verify(c => c.GetAsync("http://unittest/"), Times.Exactly(2));
        }
        
        [Fact]
        public async Task Caches_per_url()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
            cache.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(null);
            cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>())).Returns(Task.FromResult(true));
            var client = new HttpClient(new RedisCacheHandler(testMessageHandler, new Dictionary<HttpStatusCode, TimeSpan>(), cache.Object));

            // execute for different URLs
            await client.GetAsync("http://unittest1");
            await client.GetAsync("http://unittest2");

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(2);
            cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Exactly(2));
        }
        
        [Fact]
        public async Task Only_caches_get_results()
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
            cache.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(savedData);
            cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>())).Callback((string key, byte[] data, DistributedCacheEntryOptions o) => savedData = data)
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

            cache.Setup(c => c.GetAsync("http://unittest/")).ReturnsAsync(null);
            cache.Setup(c => c.SetAsync("http://unittest/", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>())).Returns(Task.FromResult(true));

            var cacheExpirationPerStatusCode = new Dictionary<HttpStatusCode, TimeSpan>();

            cacheExpirationPerStatusCode.Add((HttpStatusCode)200, TimeSpan.FromSeconds(0));

            var client = new HttpClient(new RedisCacheHandler(testMessageHandler, cacheExpirationPerStatusCode, cache.Object));

            // execute twice
            await client.GetAsync("http://unittest");
            await client.GetAsync("http://unittest");

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(2);
            cache.Verify(c => c.SetAsync("http://unittest/", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Exactly(0));
        }
    }
}