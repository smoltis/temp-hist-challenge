using System;

namespace TemperatureHistogramChallenge.Services
{
    public class FeedException : Exception
    {
        public string SourceLocation { get; }

        public int StatusCode { get; }

        public FeedException(string sourceLocation, int statusCode, string message) : base(message)
        {
            SourceLocation = sourceLocation;
            StatusCode = statusCode;
        }

        public FeedException(string sourceLocation, int statusCode, string message, Exception innerException)
            : base(message, innerException)
        {
            SourceLocation = sourceLocation;
            StatusCode = statusCode;
        }
    }
}
