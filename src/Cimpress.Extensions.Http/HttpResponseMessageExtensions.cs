using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Cimpress.Extensions.Http
{
    public static class HttpResponseMessageExtensions
    {
        /// <summary>
        /// Logs a message using the injected logger and throws an exception when the status code indicates an unsuccessful response.
        /// </summary>
        /// <returns>Returns an awaitable Task.</returns>
        /// <param name="message">Extension on a HttpResponseMessage.</param>
        /// <param name="logger">Logger used to execute logging.</param>
        /// <exception cref="NotSuccessHttpResponseException">Thrown when not success status code</exception>
        public static async Task LogAndThrowIfNotSuccessStatusCode(this HttpResponseMessage message, ILogger logger)
        {
            if (!message.IsSuccessStatusCode)
            {
                var formattedMsg = await LogMessage(message, logger);
                throw new NotSuccessHttpResponseException(formattedMsg, message.StatusCode);
            }
        }

        /// <summary>
        /// Logs a message using the injected logger when the status code indicates an unsuccessful response.
        /// </summary>
        /// <param name="message">Extension on a HttpResponseMessage.</param>
        /// <returns>Returns an awaitable Task.</returns>
        /// <param name="logger">Logger used to execute logging.</param>
        public static async Task LogIfNotSuccessStatusCode(this HttpResponseMessage message, ILogger logger)
        {
            if (!message.IsSuccessStatusCode)
            {
                await LogMessage(message, logger);
            }
        }

        /// <summary>
        /// Throws an exception when the status code indicates an unsuccessful response.
        /// </summary>
        /// <param name="message">Extension on a HttpResponseMessage.</param>
        /// <returns>Returns an awaitable Task.</returns>
        /// <exception cref="NotSuccessHttpResponseException">Thrown when not success status code</exception>
        public static async Task ThrowIfNotSuccessStatusCode(this HttpResponseMessage message)
        {
            if (!message.IsSuccessStatusCode)
            {
                var formattedMsg = await FormatErrorMessage(message);
                throw new NotSuccessHttpResponseException(formattedMsg, message.StatusCode);
            }
        }

        /// <summary>
        /// Logs a message using the injected logger.
        /// </summary>
        /// <param name="message">Extension on a HttpResponseMessage.</param>
        /// <param name="logger">The logger to use logging this message to.</param>
        /// <returns>Returns a formatted version of the log message.</returns>
        public static async Task<string> LogMessage(HttpResponseMessage message, ILogger logger)
        {
            var content = await message.Content.ReadAsStringAsync();
            logger.LogError("Error processing request: {StatusCode} {RequestUri} {Content}", message.StatusCode, message.RequestMessage.RequestUri, content);
            return $"Error processing request. Status code was {message.StatusCode} when calling '{message.RequestMessage.RequestUri}', message was '{content}'";
        }

        public static async Task<string> FormatErrorMessage(this HttpResponseMessage message)
        {
            var msg = await message.Content.ReadAsStringAsync();
            var formattedMsg = $"Error processing request. Status code was {message.StatusCode} when calling '{message.RequestMessage.RequestUri}', message was '{msg}'";
            return formattedMsg;
        }
    }
}
