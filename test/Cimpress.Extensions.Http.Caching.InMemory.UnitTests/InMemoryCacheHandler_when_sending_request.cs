using System;
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
            cache.Remove(new Uri("http://unittest"));
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
        public async Task Only_caches_get_results()
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
    }
}