using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Cimpress.Extensions.Http.Caching.Abstractions;
using Cimpress.Extensions.Http.Caching.Redis.Microsoft;
using FluentAssertions;
using Xunit;

namespace Cimpress.Extensions.Http.Caching.Redis.Examples
{
    public class Redis_basic_example
    {
        [Fact]
        public async Task Tests_redis_live_connection()
        {
            const string url = "http://thecatapi.com/api/images/get?format=html";

            RedisCacheOptions options = new RedisCacheOptions
            {
                Configuration = "localhost",
                InstanceName = "example-tests" + Guid.NewGuid() // create a new instance name to ensure a unique key naming to have consistent test results
            };

            var handler = new RedisCacheHandler(new HttpClientHandler(), CacheExpirationProvider.CreateSimple(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5)), options);
            using (var client = new HttpClient(handler))
            {
                for (int i = 0; i < 5; i++)
                {
                    var sw = Stopwatch.StartNew();
                    Debug.Write($"Getting data from {url}, iteration #{i + 1}...");
                    var result = await client.GetAsync(url);
                    var content = await result.Content.ReadAsStringAsync();
                    Debug.WriteLine($" completed in {sw.ElapsedMilliseconds}ms. Content was {content}.");
                }
            }

            StatsResult stats = handler.StatsProvider.GetStatistics();
            stats.Total.CacheHit.Should().Be(4);
            stats.Total.CacheMiss.Should().Be(1);
        }

        [Fact]
        public async Task Tests_cache_invalidation()
        {
            const string url = "http://thecatapi.com/api/images/get?format=html";

            RedisCacheOptions options = new RedisCacheOptions
            {
                Configuration = "localhost",
                InstanceName = "example-tests" + Guid.NewGuid() // create a new instance name to ensure a unique key naming to have consistent test results
            };

            var handler = new RedisCacheHandler(new HttpClientHandler(), CacheExpirationProvider.CreateSimple(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5)), options);
            using (var client = new HttpClient(handler))
            {
                for (int i = 0; i < 5; i++)
                {
                    var sw = Stopwatch.StartNew();
                    Debug.Write($"Getting data from {url}, iteration #{i + 1}...");
                    var result = await client.GetAsync(url);
                    var content = await result.Content.ReadAsStringAsync();
                    Debug.WriteLine($" completed in {sw.ElapsedMilliseconds}ms. Content was {content}.");
                    if (i % 2 == 0)
                    {
                        Debug.WriteLine($"Iteration {i}. Invalidating cache.");
                        await handler.InvalidateCache(new Uri(url));
                    }
                }
            }

            StatsResult stats = handler.StatsProvider.GetStatistics();
            stats.Total.CacheHit.Should().Be(2);
            stats.Total.CacheMiss.Should().Be(3);
        }
    }
}
