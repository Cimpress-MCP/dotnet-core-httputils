using System.Net;
using FluentAssertions;
using Xunit;

namespace Cimpress.Extensions.Http.Caching.Abstractions.UnitTests
{
    public class StatsResult_when_calculating
    {
        [Fact]
        public void Calculates_total_correctly()
        {
            var stats = new StatsResult("unit-test");

            long hits = 0;
            long misses = 0;

            for (int i = 0; i < 10; i++)
            {
                stats.PerStatusCode.Add((HttpStatusCode) 400 + i, new StatsValue {CacheHit = 2 * i, CacheMiss = i});
                hits += 2 * i;
                misses += i;
            }
            stats.Total.CacheHit.Should().Be(hits);
            stats.Total.CacheMiss.Should().Be(misses);
        }
    }
}