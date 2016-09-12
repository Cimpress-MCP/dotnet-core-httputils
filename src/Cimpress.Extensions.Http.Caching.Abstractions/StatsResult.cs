using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Cimpress.Extensions.Http.Caching.Abstractions
{
    public class StatsResult
    {
        public StatsResult(string cacheType)
        {
            CacheType = cacheType;
            PerStatusCode = new Dictionary<HttpStatusCode, StatsValue>();

            // using Now instead of UtcNow to make it easier to read in local time, but still including the timezone offset
            StatsCreatedAt = DateTimeOffset.Now;
        }

        /// <summary>
        /// The cache type, in order to distinguish between various caching strategies such as InMemory caching or Redis based caching.
        /// </summary>
        public string CacheType { get; }

        /// <summary>
        /// The time when these stats have been created.
        /// </summary>
        public DateTimeOffset StatsCreatedAt { get; }

        /// <summary>
        /// The statistics per status code.
        /// </summary>
        public Dictionary<HttpStatusCode, StatsValue> PerStatusCode { get; set; }

        /// <summary>
        /// The summary of <see cref="PerStatusCode"/> statistics, providing a simple overview.
        /// </summary>
        public StatsValue Total => new StatsValue {CacheHit = PerStatusCode.Sum(v => v.Value.CacheHit), CacheMiss = PerStatusCode.Sum(v => v.Value.CacheMiss)};
    }
}