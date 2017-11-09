using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cimpress.Extensions.Http.MessageHandlers;
using FluentAssertions;
using Xunit;

namespace Cimpress.Extensions.Http.UnitTests.MessageHandlers
{
    public class ConcurrencyLimitingHandler_when_limiting_concurrency
    {
        private class TestHandler : DelegatingHandler
        {
            private readonly TimeSpan executionTime = TimeSpan.FromMilliseconds(10);
            private int numberOfExecutions;
            private readonly int maxNumberOfExecutions;
            
            public int MaxSeenNumberOfExecutions { get; private set; }

            public TestHandler(int maxNumberOfExecutions)
            {
                this.maxNumberOfExecutions = maxNumberOfExecutions;
            }
            
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                numberOfExecutions++;
                MaxSeenNumberOfExecutions = Math.Max(numberOfExecutions, MaxSeenNumberOfExecutions);
                if (numberOfExecutions > maxNumberOfExecutions)
                {
                    throw new Exception("Max number of execution reached.");
                }
                await Task.Delay(executionTime, cancellationToken);
                var msg = new HttpResponseMessage(HttpStatusCode.OK);
                numberOfExecutions--;
                return msg;
            }
        }

        public static IEnumerable<object[]> TestData
        {
            get
            {
                var urls = Enumerable.Range(0, 100).Select(r => new Uri($"http://unit.test/{r}"));
                yield return new object[] {urls, 10};
                yield return new object[] {urls, 5};
                yield return new object[] {urls, 20};
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void Does_not_exceed_concurrency(IEnumerable<Uri> uris, int maxConcurrency)
        {
            // setup
            var testHandler = new TestHandler(maxConcurrency);
            var handler = new ConcurrencyLimitingMessageHandler(maxConcurrency, testHandler);
            var client = new HttpClient(handler);

            // execute
            var parallelRequests = uris.Select(uri => client.GetAsync(uri));
            Func<Task> exec = async () => await Task.WhenAll(parallelRequests);
            
            // validate
            // when validating the maximal seen number of execution, let's allow for some error in case the
            // test ran a bit slower. Usually it should match though.
            exec.ShouldNotThrow($"Error while executing with max concurrency of {maxConcurrency}");
            testHandler.MaxSeenNumberOfExecutions.Should().BeInRange(maxConcurrency - 2, maxConcurrency);
        }

        [Fact]
        public void Respects_cancellation_token()
        {
            // setup
            var handler = new ConcurrencyLimitingMessageHandler(0, new TestHandler(0));
            var client = new HttpClient(handler) {Timeout = TimeSpan.FromMilliseconds(1)};

            // execute
            Func<Task> exec = async () => await client.GetAsync("http://localhost/bar");

            // validate
            exec.ShouldThrow<OperationCanceledException>();
        }
    }
}