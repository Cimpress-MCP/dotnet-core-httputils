using Microsoft.Extensions.Options;

namespace Cimpress.Extensions.Http.Caching.Redis.Microsoft
{
    /// <summary>
    /// Configuration options for <see cref="RedisCache"/>.
    /// </summary>
    /// <remarks>
    /// This is a copy of https://github.com/aspnet/Caching/tree/dev/src/Microsoft.Extensions.Caching.Redis and will be removed from this
    /// repository as soon as Microsoft.Extensions.Caching.Redis support .NET Core and we can depend on that library directly.
    /// See https://github.com/aspnet/Caching/pull/147 as a starting point of related discussions.
    /// </remarks>
    public class RedisCacheOptions : IOptions<RedisCacheOptions>
    {
        /// <summary>
        /// The configuration used to connect to Redis.
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// The Redis instance name.
        /// </summary>
        public string InstanceName { get; set; }

        RedisCacheOptions IOptions<RedisCacheOptions>.Value
        {
            get { return this; }
        }
    }
}