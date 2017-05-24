using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace Cimpress.Extensions.Http.Caching.InMemory.UnitTests
{
    public class InMemoryCacheFallbackHandler_when_sending_request
    {
        private string url = "http://unittest/";

        [Fact]
        public async Task Always_calls_the_http_handler()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var client = new HttpClient(new InMemoryCacheFallbackHandler(testMessageHandler, TimeSpan.FromDays(1), TimeSpan.FromDays(1), null, cache));

            // execute twice
            await client.GetAsync(url);
            cache.Get(url).Should().NotBeNull(); // ensure it's cached before the 2nd call
            await client.GetAsync(url);

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(2);
        }

        [Fact]
        public async Task Always_updates_the_cache_on_success()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new Mock<IMemoryCache>(MockBehavior.Strict);
            var cacheTime = TimeSpan.FromSeconds(123);
            cache.Setup(c => c.CreateEntry(url));
            var client = new HttpClient(new InMemoryCacheFallbackHandler(testMessageHandler, TimeSpan.FromDays(1), cacheTime, null, cache.Object));

            // execute twice, validate cache is called each time
            await client.GetAsync(url);
            cache.Verify(c => c.CreateEntry(url), Times.Once);
            await client.GetAsync(url);
            cache.Verify(c => c.CreateEntry(url), Times.Exactly(2));
        }

        [Fact]
        public async Task Never_updates_the_cache_on_failure()
        {
            // setup
            var testMessageHandler = new TestMessageHandler(HttpStatusCode.InternalServerError);
            var cache = new Mock<IMemoryCache>(MockBehavior.Strict);
            var cacheTime = TimeSpan.FromSeconds(123);
            object expectedValue;
            cache.Setup(c => c.CreateEntry(It.IsAny<string>()));
            cache.Setup(c => c.TryGetValue(url, out expectedValue)).Returns(false);
            var client = new HttpClient(new InMemoryCacheFallbackHandler(testMessageHandler, TimeSpan.FromDays(1), cacheTime, null, cache.Object));

            // execute
            await client.GetAsync(url);
            
            // validate
            cache.Verify(c => c.CreateEntry(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Tries_to_access_cache_on_failure_but_returns_error_if_not_in_cache()
        {
            // setup
            var testMessageHandler = new TestMessageHandler(HttpStatusCode.InternalServerError);
            var cache = new Mock<IMemoryCache>(MockBehavior.Strict);
            var cacheTime = TimeSpan.FromSeconds(123);
            object expectedValue;
            cache.Setup(c => c.TryGetValue(url, out expectedValue)).Returns(false);
            var client = new HttpClient(new InMemoryCacheFallbackHandler(testMessageHandler, TimeSpan.FromDays(1), cacheTime, null, cache.Object));

            // execute
            var result = await client.GetAsync(url);

            // validate
            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task Gets_it_from_the_http_call_after_being_in_cache()
        {
            // setup
            var testMessageHandler1 = new TestMessageHandler(content: "message-1", delay: TimeSpan.FromMilliseconds(100));
            var testMessageHandler2 = new TestMessageHandler(content: "message-2");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var client1 = new HttpClient(new InMemoryCacheFallbackHandler(testMessageHandler1, TimeSpan.FromMilliseconds(1), TimeSpan.FromDays(1), null, cache));
            var client2 = new HttpClient(new InMemoryCacheFallbackHandler(testMessageHandler2, TimeSpan.FromMilliseconds(1), TimeSpan.FromDays(1), null, cache));

            // execute twice
            var result1 = await client1.GetAsync(url);
            cache.Get(url).Should().NotBeNull();
            var result2 = await client2.GetAsync(url);

            // validate
            // - that each message handler got called
            testMessageHandler1.NumberOfCalls.Should().Be(1);
            testMessageHandler2.NumberOfCalls.Should().Be(1);

            // - that the 2nd result got served from the http call
            var data1 = await result1.Content.ReadAsStringAsync();
            var data2 = await result2.Content.ReadAsStringAsync();
            data1.Should().BeEquivalentTo("message-1");
            data2.Should().BeEquivalentTo("message-2");
        }

        [Fact]
        public async Task Gets_it_from_the_cache_when_unsuccessful()
        {
            // setup
            var testMessageHandler1 = new TestMessageHandler(HttpStatusCode.OK, "message-1");
            var testMessageHandler2 = new TestMessageHandler(HttpStatusCode.BadRequest, "message-2");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var client1 = new HttpClient(new InMemoryCacheFallbackHandler(testMessageHandler1, TimeSpan.FromDays(1), TimeSpan.FromDays(1), null, cache));
            var client2 = new HttpClient(new InMemoryCacheFallbackHandler(testMessageHandler2, TimeSpan.FromDays(1), TimeSpan.FromDays(1), null, cache));

            // execute twice
            var result1 = await client1.GetAsync(url);
            var result2 = await client2.GetAsync(url);

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