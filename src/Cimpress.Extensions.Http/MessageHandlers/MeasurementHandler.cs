using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cimpress.Extensions.Http.MessageHandlers
{
    /// <summary>
    /// Observes a service call and logs invocation time and outcome.
    /// </summary>
    public class MeasurementHandler : DelegatingHandler
    {
        public ILogger Logger { get; }

        /// <summary>
        /// Creates a new measurement handler.
        /// </summary>
        /// <param name="logger">The logger where to send the log message to after a request has been completed.</param>
        /// <param name="innerHandler">The optional inner handler.</param>
        public MeasurementHandler(ILogger logger, HttpMessageHandler innerHandler = null) : base(innerHandler ?? new HttpClientHandler())
        {
            Logger = logger;
        }

        /// <summary>
        /// Measures invocation time of the underlying service call and logs it.
        /// </summary>
        /// <returns></returns>
        /// <remarks>This method logs failed HTTP calls, but does not perform any exception handling
        /// - it's up to the calling client to handle erroneous code.</remarks>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            var sendTask = base.SendAsync(request, cancellationToken);

            // schedule the continuation without ever awaiting it
            sendTask.ContinueWith(t =>
            {
                sw.Stop();
                var r = t.Result;
                var method = request.Method.Method;
                var uri = request.RequestUri;

                HttpStatusCode status = 0;
                string result = null;
                if (t.IsCompleted)
                {
                    status = r.StatusCode;
                    result = r.IsSuccessStatusCode ? "completed successfully" : "failed";
                }
                else if (t.IsCanceled)
                {
                    result = "canceled";
                }
                else if (t.IsFaulted)
                {
                    result = "faulted";
                }

                //generates log entry like: "HTTP GET at http://localhost/foo.bar failed with status NotFound in 19ms."
                var message = "HTTP {HttpMethod} at {ServiceUri} {HttpResult} with status {HttpStatus} in {ElapsedMilliseconds}ms.";
                Logger.LogInformation(message, method, uri, result, status, sw.ElapsedMilliseconds);
            }, cancellationToken);

            return sendTask;
        }
    }
}
