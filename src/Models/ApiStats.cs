using System.Collections.Generic;

namespace TemperatureHistogramChallenge.Models
{
    public class ApiStats
    {
        public int TotalCalls { get; set; }
        public int FailedCalls { get; set; }

        public Dictionary<ApiFailReason, int> FailReasons { get; set; }
    }

    public enum ApiFailReason
    {
        MissingData,
        FailedLookup,
        Other
    }
}