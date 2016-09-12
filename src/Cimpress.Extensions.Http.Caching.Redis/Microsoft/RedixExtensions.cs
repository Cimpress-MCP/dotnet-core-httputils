using System.Threading.Tasks;
using StackExchange.Redis;

namespace Cimpress.Extensions.Http.Caching.Redis.Microsoft
{
    /// <remarks>
    /// This is a copy of https://github.com/aspnet/Caching/tree/dev/src/Microsoft.Extensions.Caching.Redis and will be removed from this
    /// repository as soon as Microsoft.Extensions.Caching.Redis support .NET Core and we can depend on that library directly.
    /// See https://github.com/aspnet/Caching/pull/147 as a starting point of related discussions.
    /// </remarks>
    internal static class RedisExtensions
    {
        private const string HmGetScript = (@"return redis.call('HMGET', KEYS[1], unpack(ARGV))");

        internal static RedisValue[] HashMemberGet(this IDatabase cache, string key, params string[] members)
        {
            var result = cache.ScriptEvaluate(
                HmGetScript,
                new RedisKey[] { key },
                GetRedisMembers(members));

            // TODO: Error checking?
            return (RedisValue[])result;
        }

        internal static async Task<RedisValue[]> HashMemberGetAsync(
            this IDatabase cache,
            string key,
            params string[] members)
        {
            var result = await cache.ScriptEvaluateAsync(
                HmGetScript,
                new RedisKey[] { key },
                GetRedisMembers(members));

            // TODO: Error checking?
            return (RedisValue[])result;
        }

        private static RedisValue[] GetRedisMembers(params string[] members)
        {
            var redisMembers = new RedisValue[members.Length];
            for (int i = 0; i < members.Length; i++)
            {
                redisMembers[i] = (RedisValue)members[i];
            }

            return redisMembers;
        }
    }
}
