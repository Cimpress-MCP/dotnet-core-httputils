using System;
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
        private readonly Action<MeasurementDetails> logFunc;
        private readonly ILogger logger;

        /// <summary>
        /// Creates a new measurement handler.
        /// </summary>
        /// <param name="logger">The logger where to send the log message to after a request has been completed.</param>
        /// <param name="innerHandler">The optional inner handler.</param>
        /// <param name="logFunc">A function to log information; defaults to a standard implementation.</param>
        public MeasurementHandler(ILogger logger = null, HttpMessageHandler innerHandler = null, Action<MeasurementDetails> logFunc = null) : base(innerHandler ?? new HttpClientHandler())
        {
            this.logFunc = logFunc ?? Log;
            this.logger = logger;
        }

        /// <summary>
        /// Measures invocation time of the underlying service call and logs it.
        /// </summary>
        /// <returns></returns>
        /// <remarks>This method logs failed HTTP calls, but does not perform any exception handling
        /// - it's up to the calling client to handle erroneous code.</remarks>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // start measurement
            var sw = Stopwatch.StartNew();

            // start the task of executing the request
            var sendTask = base.SendAsync(request, cancellationToken);

            // schedule the continuation without ever awaiting it
            sendTask.ContinueWith(t => { logFunc(new MeasurementDetails(request, t, sw.ElapsedMilliseconds)); }, cancellationToken);

            // return the send task
            return sendTask;
        }

        private void Log(MeasurementDetails details)
        {
            var method = details.Request.Method.Method;
            var uri = details.Request.RequestUri;

            HttpStatusCode status = 0;
            string result = null;
            if (details.SendTask.IsCompleted)
            {
                var r = details.SendTask.Result;
                status = r.StatusCode;
                result = r.IsSuccessStatusCode ? "completed successfully" : "failed";
            }
            else if (details.SendTask.IsCanceled)
            {
                result = "canceled";
            }
            else if (details.SendTask.IsFaulted)
            {
                result = "faulted";
            }

            //generates log entry like: "HTTP GET at http://localhost/foo.bar failed with status NotFound in 19ms."
            var message = "HTTP {HttpMethod} at {ServiceUri} {HttpResult} with status {HttpStatus} in {ElapsedMilliseconds}ms.";
            logger.LogInformation(message, method, uri, result, status, details.ElapsedMilliseconds);
        }
    }
}
