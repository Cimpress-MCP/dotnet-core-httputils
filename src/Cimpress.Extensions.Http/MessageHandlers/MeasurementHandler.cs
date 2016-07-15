using System.Diagnostics;
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

        public MeasurementHandler(ILogger logger) : base(new HttpClientHandler())
        {
            Logger = logger;
        }

        /// <summary>
        /// Measures invocation time of the underlying service call and logs it.
        /// </summary>
        /// <returns></returns>
        /// <remarks>This method logs failed HTTP calls, but does not perform any exception handling
        /// - it's up to the calling client to handle erroneous code.</remarks>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            sw.Stop();
            var method = response.RequestMessage.Method.Method;
            var uri = response.RequestMessage.RequestUri;
            var status = response.StatusCode;
            bool isSuccess = response.IsSuccessStatusCode;

            //generates log entry like: "HTTP GET at http://localhost/foo.bar failed with status NotFound in 19ms."

            string result = isSuccess ? "completed successfully" : "failed";
            var message = "HTTP {HttpMethod} at {ServiceUri} {HttpResult} with status {HttpStatus} in {ElapsedMilliseconds}ms.";
            Logger.LogInformation(message, method, uri, result, status, sw.ElapsedMilliseconds);

            return response;
        }
    }
}
