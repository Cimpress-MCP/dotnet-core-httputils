using System;

namespace Cimpress.Extensions.Http
{
    public class NotSuccessHttpResponseException : Exception
    {
        public NotSuccessHttpResponseException(string message) : base(message) {}
    }
}
