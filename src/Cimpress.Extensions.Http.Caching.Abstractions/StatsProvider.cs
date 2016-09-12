using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace Cimpress.Extensions.Http.Caching.Abstractions
{
    /// <summary>
    /// Simple implementation of an <see cref="IStatsProvider"/>.
    /// </summary>
    public class StatsProvider : IStatsProvider
    {
        private readonly string cacheType;
        private readonly ConcurrentDictionary<HttpStatusCode, StatsValue> values;

        public StatsProvider(string cacheType)
        {
            this.cacheType = cacheType;
            values = new ConcurrentDictionary<HttpStatusCode, StatsValue>();
        }

        public void ReportCacheHit(HttpStatusCode statusCode)
        {
            values.AddOrUpdate(statusCode, _ => new StatsValue {CacheHit = 1}, (_, existing) =>
            {
                existing.CacheHit++;
                return existing;
            });
        }

        public void ReportCacheMiss(HttpStatusCode statusCode)
        {
            values.AddOrUpdate(statusCode, _ => new StatsValue { CacheMiss = 1 }, (_, existing) =>
            {
                existing.CacheMiss++;
                return existing;
            });
        }

        public StatsResult GetStatistics()
        {
            return new StatsResult(cacheType) {PerStatusCode = new Dictionary<HttpStatusCode, StatsValue>(values)};
        }
    }
}