using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Cimpress.Extensions.Http.Caching.Abstractions;

namespace Cimpress.Extensions.Http.Caching.InMemory.Examples
{
    public class InMemory_cache_fallback_example
    {
        [Theory]
        [InlineData(10, 4, 1, 2000)] // timeout always occurs, but from the 2nd instance onwards we should get served from the cache
        [InlineData(5000, 0, 5, 1)] // timeout shouldn't occur; always get it from HTTP
        public async Task Tests_in_memory_cache_fallback_functionality(int maxTimeoutInMs, int cacheHits, int cacheMisses, int timeoutBetweenExecutions)
        {
            const string url = "http://thecatapi.com/api/images/get?format=html";
            
            var handler = new InMemoryCacheFallbackHandler(new HttpClientHandler(), TimeSpan.FromMilliseconds(maxTimeoutInMs), TimeSpan.FromDays(100));
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
