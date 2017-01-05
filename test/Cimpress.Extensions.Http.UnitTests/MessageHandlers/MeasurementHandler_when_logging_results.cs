using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cimpress.Extensions.Http.MessageHandlers;
using FluentAssertions;
using Xunit;

namespace Cimpress.Extensions.Http.UnitTests.MessageHandlers
{
    public class MeasurementHandler_when_logging_results
    {
        private class TestHandler : DelegatingHandler
        {
            private readonly TimeSpan executionTime;
            private readonly HttpStatusCode statusCode;
            private readonly bool throwException;

            public TestHandler(TimeSpan executionTime, HttpStatusCode statusCode, bool throwException)
            {
                this.executionTime = executionTime;
                this.statusCode = statusCode;
                this.throwException = throwException;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(executionTime, cancellationToken);
                if (throwException)
                {
                    throw new Exception("unit test");
                }
                return new HttpResponseMessage(statusCode);
            }
        }

        public static IEnumerable<object[]> LogInput
        {
            get
            {
                yield return new object[] {new Uri("http://unit.test"), HttpStatusCode.OK, false, true, false, false};
                yield return new object[] {new Uri("http://unit.test/foo/bar"), HttpStatusCode.BadRequest, false, true, false, false};
                yield return new object[] {new Uri("http://unit.test/xyz.html"), HttpStatusCode.OK, true, true, true, false};
            }
        }

        [Theory]
        [MemberData(nameof(LogInput))]
        public async Task Only_logs_when_completed(Uri uri, HttpStatusCode statusCode, bool throwException, bool isCompleted, bool isFaulted, bool isCanceled)
        {
            // setup
            var resetEvent = new ManualResetEvent(false);
            Action<MeasurementDetails> logFunc = details =>
            {
                // validate
                details.Request.Method.Method.Should().Be("GET");
                details.Request.RequestUri.Should().Be(uri);
                details.SendTask.IsCompleted.Should().Be(isCompleted);
                details.SendTask.IsFaulted.Should().Be(isFaulted);
                details.SendTask.IsCanceled.Should().Be(isCanceled);
                if (!throwException)
                {
                    details.SendTask.Result.StatusCode.Should().Be(statusCode);
                }
                resetEvent.Set();
            };
            var handler = new MeasurementHandler(null, new TestHandler(TimeSpan.FromMilliseconds(100), statusCode, throwException), logFunc);
            var client = new HttpClient(handler);

            // execute
            try
            {
                await client.GetAsync(uri);
            }
            catch (Exception)
            {
                if (!throwException) throw;
            }

            // wait until the validation has completed (but limit to max 1 second to avoid waiting forever)
            bool awaited = resetEvent.WaitOne(TimeSpan.FromSeconds(1));
            awaited.Should().BeTrue();
        }
    }
}