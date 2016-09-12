using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Cimpress.Extensions.Http.Caching.Abstractions;
using Cimpress.Extensions.Http.Caching.InMemory;

namespace Cimpress.Extensions.Http.Caching.InMemory.Examples
{
    public class InMemory_basic_example
    {
        [Fact]
        public async Task Tests_in_memory_functionality()
        {
            const string url = "http://thecatapi.com/api/images/get?format=html";
            
            var handler = new InMemoryCacheHandler(new HttpClientHandler(), CacheExpirationProvider.CreateSimple(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5)));
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
    }
}
