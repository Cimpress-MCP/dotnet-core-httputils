using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Cimpress.Extensions.Http.Caching.InMemory.UnitTests
{
    public class InMemoryCacheHandler_when_sending_request
    {
        [Fact]
        public async Task Caches_the_result()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var client = new HttpClient(new InMemoryCacheHandler(testMessageHandler));

            // execute twice
            await client.GetAsync("http://unittest");
            await client.GetAsync("http://unittest");

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(1);
        }

        [Fact]
        public async Task Gets_the_data_again_after_entry_is_gone_from_cache()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var client = new HttpClient(new InMemoryCacheHandler(testMessageHandler, null, null, cache));

            // execute twice
            await client.GetAsync("http://unittest");
            cache.Remove(HttpMethod.Get + new Uri("http://unittest").ToString());
            await client.GetAsync("http://unittest");

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(2);
        }

        [Fact]
        public async Task Is_case_sensitive()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var client = new HttpClient(new InMemoryCacheHandler(testMessageHandler, null, null, cache));

            // execute for different URLs, only different by casing
            await client.GetAsync("http://unittest/foo.html");
            await client.GetAsync("http://unittest/FOO.html");

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(2);
        }
        
        [Fact]
        public async Task Caches_per_url()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var client = new HttpClient(new InMemoryCacheHandler(testMessageHandler, null, null, cache));

            // execute for different URLs
            await client.GetAsync("http://unittest1");
            await client.GetAsync("http://unittest2");

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(2);
        }
        
        [Fact]
        public async Task Only_caches_get_and_head_results()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var client = new HttpClient(new InMemoryCacheHandler(testMessageHandler, null, null, cache));

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
        public async Task Caches_head_and_get_request_without_conflict()
        {
            var testMessageHandler = new TestMessageHandler();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var client = new HttpClient(new InMemoryCacheHandler(testMessageHandler, null, null, cache));

            // execute twice for different methods
            await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "http://unittest"));
            await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://unittest"));

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(2);
        }
        
        [Fact]
        public async Task Data_from_call_matches_data_from_cache()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var client = new HttpClient(new InMemoryCacheHandler(testMessageHandler, null, null, cache));

            // execute twice for different methods
            var originalResult = await client.GetAsync("http://unittest");
            var cachedResult = await client.GetAsync("http://unittest");
            var originalResultString = await originalResult.Content.ReadAsStringAsync();
            var cachedResultString = await cachedResult.Content.ReadAsStringAsync();

            // validate
            originalResultString.ShouldBeEquivalentTo(cachedResultString);
            originalResultString.ShouldBeEquivalentTo(TestMessageHandler.DefaultContent);
        }

        [Fact]
        public async Task Returns_response_header()
        {
            // setup
            var testMessageHandler = new TestMessageHandler(System.Net.HttpStatusCode.OK, "test content", "text/plain", System.Text.Encoding.UTF8);
            var client = new HttpClient(new InMemoryCacheHandler(testMessageHandler));

            // execute 
            HttpResponseMessage response = await client.GetAsync("http://unittest");

            // validate
            response.Content.Headers.ContentType.MediaType.Should().Be("text/plain");
            response.Content.Headers.ContentType.CharSet.Should().Be("utf-8");
        }

        [Fact]
        public async Task Disable_cache_per_statusCode()
        {
            // setup
            var cacheExpirationPerStatusCode = new Dictionary<HttpStatusCode, TimeSpan>();

            cacheExpirationPerStatusCode.Add((HttpStatusCode)200, TimeSpan.FromSeconds(0));

            var testMessageHandler = new TestMessageHandler();
            var client = new HttpClient(new InMemoryCacheHandler(testMessageHandler, cacheExpirationPerStatusCode));

            // execute twice
            await client.GetAsync("http://unittest");
            await client.GetAsync("http://unittest");

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(2);
        }

        [Fact]
        public async Task Invalidates_cache_correctly()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var handler = new InMemoryCacheHandler(testMessageHandler);
            var client = new HttpClient(handler);

            // execute twice, with cache invalidation in between
            var uri = new Uri("http://unittest");
            await client.GetAsync(uri);
            handler.InvalidateCache(uri);
            await client.GetAsync(uri);

            // validate
            testMessageHandler.NumberOfCalls.Should().Be(2);
        }

        [Fact]
        public async Task Invalidates_cache_per_method()
        {
            // setup
            var testMessageHandler = new TestMessageHandler();
            var handler = new InMemoryCacheHandler(testMessageHandler);
            var client = new HttpClient(handler);

            // execute with two methods, and clean up one cache
            var uri = new Uri("http://unittest");
            await client.GetAsync(uri);
            await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));
            testMessageHandler.NumberOfCalls.Should().Be(2);
            
            // clean cache
            handler.InvalidateCache(uri, HttpMethod.Head);

            // execute both actions, and only one should be retrieved from cache
            await client.GetAsync(uri);
            await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));
            testMessageHandler.NumberOfCalls.Should().Be(3);
        }
    }
}