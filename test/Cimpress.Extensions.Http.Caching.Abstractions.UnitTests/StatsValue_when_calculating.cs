using FluentAssertions;
using Xunit;

namespace Cimpress.Extensions.Http.Caching.Abstractions.UnitTests
{
    public class StatsValue_when_calculating
    {
        [Fact]
        public void Calculates_total_correctly()
        {
            // setup
            var stats = new StatsValue {CacheHit = 100, CacheMiss = 50};
            
            // validate
            stats.TotalRequests.Should().Be(150);
        }

        [Theory]
        [InlineData(100, 0, 1)]
        [InlineData(0, 154, 0)]
        [InlineData(10, 10, 0.5)]
        [InlineData(30, 10, 0.75)]
        public void Calculates_hits_percent_correctly(long hits, long misses, double percent)
        {
            // setup
            var stats = new StatsValue { CacheHit = hits, CacheMiss = misses };

            // validate
            stats.CacheHitsPercent.Should().Be(percent);
        }

        [Theory]
        [InlineData(100, 0, 0)]
        [InlineData(0, 154, 1)]
        [InlineData(10, 10, 0.5)]
        [InlineData(30, 10, 0.25)]
        public void Calculates_miss_percent_correctly(long hits, long misses, double percent)
        {
            // setup
            var stats = new StatsValue { CacheHit = hits, CacheMiss = misses };

            // validate
            stats.CacheMissPercent.Should().Be(percent);
        }
    }
}