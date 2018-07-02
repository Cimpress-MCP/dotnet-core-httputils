using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cimpress.Extensions.Http.MessageHandlers
{
    /// <summary>
    /// Class providing a handler which retries to send a HTTPRequest.
    /// </summary>
    /// <seealso cref="System.Net.Http.DelegatingHandler" />
    public class RetryHandler : DelegatingHandler
    {
        private readonly uint maxRetries;
        private readonly uint retryDelayMilliseconds;
        private readonly ILogger logger;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RetryHandler" /> class.
        /// </summary>
        /// <param name="innerHandler">The inner handler. (Usually new HttpClientHandler())</param>
        /// <param name="maxRetries">The maximum retries.</param>
        /// <param name="retryDelayMilliseconds">The retry delay milliseconds.</param>
        /// <param name="logger">The optional logger to log error messages to.</param>
        /// <remarks>
        /// When only the auth0 token provider is injected, the auth0 token provider should try to extract the client id from the 401 response header.
        /// </remarks>
        public RetryHandler(HttpMessageHandler innerHandler, uint maxRetries = 1, uint retryDelayMilliseconds = 500, ILogger logger = null)
            : base(innerHandler)
        {
            this.maxRetries = maxRetries;
            this.retryDelayMilliseconds = retryDelayMilliseconds;
            this.logger = logger;
        }

        /// <summary>
        /// Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.
        /// </summary>
        /// <param name="request">The HTTP request message to send to the server.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <returns>
        /// Returns <see cref="T:System.Threading.Tasks.Task`1" />. The task object representing the asynchronous operation.
        /// </returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    response = await base.SendAsync(request, cancellationToken);

                    if ((int) response.StatusCode <= 399)
                    {
                        return response;
                    }
                    
                    // log warnings unless it's the last attempt which is handled below
                    await LogUnsuccessfulRequest(request, response, i);

                    // abort immediately for client side errors and redirects, only retry on server side errors
                    if ((int) response.StatusCode <= 499)
                    {
                        return response;
                    }
                }
                catch (Exception sendException)
                {
                    LogSendException(request, i, sendException);
                }

                if (i == maxRetries)
                {
                    LogAttemptsExceeded(request, response, i);
                    break;
                }

                // extend retry interval with every loop (start with configured delay)
                await Task.Delay((int)(retryDelayMilliseconds * (i + 1)), cancellationToken);
            }

            return response;
        }

        private void LogSendException(HttpRequestMessage request, int attempt, Exception sendException)
        {
            if (logger != null)
            {
                string msg = $"Unexpected exception invoking REST service at {request.RequestUri}. This is attempt #{attempt + 1}.";
                logger.LogWarning(0, sendException, msg);
            }
        }

        private async Task LogUnsuccessfulRequest(HttpRequestMessage request, HttpResponseMessage response, int attempt)
        {
            if (logger == null)
            {
                return;
            }

            string msg = $"Error returned when invoking URL '{request.RequestUri}' with HTTP status {response?.StatusCode}.";
            var content = await TryGetContent(response);
            msg += $"This is attempt #{attempt + 1}. Response content was: '{content}'.";
            logger.LogWarning(msg);
        }

        private void LogAttemptsExceeded(HttpRequestMessage request, HttpResponseMessage response, int attempt)
        {
            logger?.LogWarning($"Maximum amount of {attempt + 1} attempts has been reached while invoking URL '{request.RequestUri}'. Returning the last response of status code {response?.StatusCode}.");
        }

        private async Task<string> TryGetContent(HttpResponseMessage response)
        {
            try
            {
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}