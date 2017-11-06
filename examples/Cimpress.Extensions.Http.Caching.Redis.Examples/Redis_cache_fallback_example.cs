using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Cimpress.Extensions.Http.Caching.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Caching.Redis;
using Xunit;

namespace Cimpress.Extensions.Http.Caching.Redis.Examples
{
    public class Redis_cache_fallback_example
    {
        [Theory]
        [InlineData(10, 4, 1, 2000)] // timeout always occurs, but from the 2nd instance onwards we should get served from the cache
        [InlineData(5000, 0, 5, 1)] // timeout shouldn't occur; always get it from HTTP
        public async Task Tests_redis_cache_fallback_functionality(int maxTimeoutInMs, int cacheHits, int cacheMisses, int timeoutBetweenExecutions)
        {
            const string url = "http://thecatapi.com/api/images/get?format=html";

            RedisCacheOptions options = new RedisCacheOptions
            {
                Configuration = "127.0.0.1",
                InstanceName = "example-tests" + Guid.NewGuid() // create a new instance name to ensure a unique key naming to have consistent test results
            };

            var handler = new RedisCacheFallbackHandler(new HttpClientHandler(), TimeSpan.FromMilliseconds(maxTimeoutInMs), TimeSpan.FromDays(100), options);
            using (var client = new HttpClient(handler))
            {
                for (int i = 0; i < 5; i++)
                {
                    var sw = Stopwatch.StartNew();
                    Debug.Write($"Getting data from {url}, iteration #{i + 1}...");
                    var result = await client.GetAsync(url);
                    var content = await result.Content.ReadAsStringAsync();
                    Debug.WriteLine($" completed in {sw.ElapsedMilliseconds}ms. Content was {content}.");
                    await Task.Delay(timeoutBetweenExecutions);
                }
            }

            StatsResult stats = handler.StatsProvider.GetStatistics();
            stats.Total.CacheHit.Should().Be(cacheHits, "cache hit mismatch");
            stats.Total.CacheMiss.Should().Be(cacheMisses, "cache miss mismatch");
        }
    }
}
