using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Cimpress.Extensions.Http.MessageHandlers
{
    /// <summary>
    /// A message handler that limits concurrency.
    /// </summary>
    public class ConcurrencyLimitingMessageHandler : DelegatingHandler
    {
        private readonly SemaphoreSlim concurrencySemaphore;

        /// <summary>
        /// Creates a new concurrency limiting message handler.
        /// </summary>
        /// <param name="maxConcurrency">The max concurrency across all requests.</param>
        public ConcurrencyLimitingMessageHandler(int maxConcurrency) : this(maxConcurrency, new HttpClientHandler()) { }

        /// <summary>
        /// Creates a new concurrency limiting message handler.
        /// </summary>
        /// <param name="maxConcurrency">The max concurrency across all requests.</param>
        /// <param name="innerHandler">The inner handler to call for executing the actual request.</param>
        public ConcurrencyLimitingMessageHandler(int maxConcurrency, HttpMessageHandler innerHandler) : base(innerHandler)
        {
            concurrencySemaphore = new SemaphoreSlim(maxConcurrency);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Get access to continue on this code
            await concurrencySemaphore.WaitAsync(cancellationToken);
            try
            {
                // call the inner handler
                return await base.SendAsync(request, cancellationToken);
            }
            finally
            {
                // release the concurrency block again
                concurrencySemaphore.Release();
            }
        }
    }
}