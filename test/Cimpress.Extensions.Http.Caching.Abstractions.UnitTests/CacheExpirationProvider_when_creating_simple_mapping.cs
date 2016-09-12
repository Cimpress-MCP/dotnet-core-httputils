using System;
using System.Net;
using FluentAssertions;
using Xunit;

namespace Cimpress.Extensions.Http.Caching.Abstractions.UnitTests
{
    public class CacheExpirationProvider_when_creating_simple_mapping
    {
        [Fact]
        public void Puts_the_correct_status_codes_into_the_mapping()
        {
            // execute
            var mappings = CacheExpirationProvider.CreateSimple(TimeSpan.FromTicks(1), TimeSpan.FromTicks(2), TimeSpan.FromTicks(3));

            // validate
            mappings.Count.Should().Be(3);
            mappings[HttpStatusCode.OK].Should().Be(TimeSpan.FromTicks(1));
            mappings[HttpStatusCode.BadRequest].Should().Be(TimeSpan.FromTicks(2));
            mappings[HttpStatusCode.InternalServerError].Should().Be(TimeSpan.FromTicks(3));
        }
    }
}