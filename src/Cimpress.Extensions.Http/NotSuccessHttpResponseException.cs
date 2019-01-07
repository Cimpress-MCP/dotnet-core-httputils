using System;
using System.Net;

namespace Cimpress.Extensions.Http
{
    public class NotSuccessHttpResponseException : Exception
    {
        HttpStatusCode StatusCode { get; }

        public NotSuccessHttpResponseException(string message, HttpStatusCode statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
