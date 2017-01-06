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
            private readonly string content;

            public TestHandler(TimeSpan executionTime, HttpStatusCode statusCode, bool throwException, string content = null)
            {
                this.executionTime = executionTime;
                this.statusCode = statusCode;
                this.throwException = throwException;
                this.content = content;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(executionTime, cancellationToken);
                if (throwException)
                {
                    throw new Exception("unit test");
                }
                var msg = new HttpResponseMessage(statusCode);
                if (content != null)
                {
                    msg.Content = new StringContent(content);
                }
                return msg;
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

        [Fact]
        public async Task Does_not_await_completion_of_log_function()
        {
            // setup
            var resetEvent = new ManualResetEvent(false);
            var hasLogged = false;
            Action<MeasurementDetails> logFunc = async details =>
            {
                // validate
                await Task.Delay(100);
                hasLogged = true;
                resetEvent.Set();
            };
            var expectedResult = Guid.NewGuid().ToString();
            var handler = new MeasurementHandler(null, new TestHandler(TimeSpan.FromMilliseconds(100), HttpStatusCode.OK, false, expectedResult), logFunc);
            var client = new HttpClient(handler);

            // execute
            var result = await client.GetStringAsync("http://unit.test");
            
            // validate that the string was retrieved, but the logger hasn't completed yet
            result.ShouldBeEquivalentTo(expectedResult);
            hasLogged.Should().BeFalse();

            // ensure logging eventually happened
            resetEvent.WaitOne(TimeSpan.FromSeconds(1));
            hasLogged.Should().BeTrue();
        }
    }
}