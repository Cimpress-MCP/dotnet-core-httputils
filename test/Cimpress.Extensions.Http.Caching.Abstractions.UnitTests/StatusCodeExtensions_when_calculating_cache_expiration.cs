using System;
using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using Xunit;

namespace Cimpress.Extensions.Http.Caching.Abstractions.UnitTests
{
    public class StatusCodeExtensions_when_calculating_cache_expiration
    {
        [Fact]
        public void Takes_value_from_Http_code()
        {
            // setup
            var ticks = new Random().Next(0, 100000);
            var code = HttpStatusCode.NotFound;
            var mappings = new Dictionary<HttpStatusCode, TimeSpan> { {code, TimeSpan.FromTicks(ticks)} };

            // execute
            TimeSpan result = code.GetAbsoluteExpirationRelativeToNow(mappings);

            // validate
            result.Should().Be(mappings[code]);
        }

        [Fact]
        public void Takes_value_from_http_code_category()
        {
            // setup
            var r = new Random();
            var mappings = CacheExpirationProvider.CreateSimple(TimeSpan.FromTicks(r.Next(0, 100000)), TimeSpan.FromTicks(r.Next(0, 100000)), TimeSpan.FromTicks(r.Next(0, 100000)));

            // execute
            TimeSpan successResult = HttpStatusCode.Created.GetAbsoluteExpirationRelativeToNow(mappings);
            TimeSpan clientErrorResult = HttpStatusCode.NotFound.GetAbsoluteExpirationRelativeToNow(mappings);
            TimeSpan serverErrorResult = HttpStatusCode.GatewayTimeout.GetAbsoluteExpirationRelativeToNow(mappings);

            // validate
            successResult.Should().Be(mappings[HttpStatusCode.OK]);
            clientErrorResult.Should().Be(mappings[HttpStatusCode.BadRequest]);
            serverErrorResult.Should().Be(mappings[HttpStatusCode.InternalServerError]);
        }

        [Fact]
        public void Takes_default_value()
        {
            // setup
            var mappings = new Dictionary<HttpStatusCode, TimeSpan>();

            // execute
            TimeSpan result = HttpStatusCode.OK.GetAbsoluteExpirationRelativeToNow(mappings);
            
            // validate
            result.Should().Be(TimeSpan.FromDays(1));
        }
    }
}
