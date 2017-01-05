using System.Net.Http;
using System.Threading.Tasks;

namespace Cimpress.Extensions.Http.MessageHandlers
{
    /// <summary>
    /// Specifying details from the measurement that get passed into a log function.
    /// </summary>
    public class MeasurementDetails
    {
        public MeasurementDetails(HttpRequestMessage request, Task<HttpResponseMessage> sendTask, long elapsedMilliseconds)
        {
            Request = request;
            SendTask = sendTask;
            ElapsedMilliseconds = elapsedMilliseconds;
        }

        /// <summary>
        /// The original HTTP request that triggered the measurement.
        /// </summary>
        public HttpRequestMessage Request { get; }

        /// <summary>
        /// The completed (failed, success, canceled) task with the response.
        /// </summary>
        public Task<HttpResponseMessage> SendTask { get; }

        /// <summary>
        /// The elapsed milliseconds to complete the <see cref="SendTask"/>.
        /// </summary>
        public long ElapsedMilliseconds { get; }
    }
}