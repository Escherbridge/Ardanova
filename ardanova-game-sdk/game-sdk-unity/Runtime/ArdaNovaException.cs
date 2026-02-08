using System;

namespace ArdaNova
{
    public class ArdaNovaException : Exception
    {
        public int StatusCode { get; }

        public ArdaNovaException(string message, int statusCode = 0)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
