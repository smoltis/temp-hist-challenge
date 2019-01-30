using System.Collections.Generic;

namespace TemperatureHistogramChallenge
{
    public class APIStats
    {
        public int TotalCalls { get; set; }
        public int FailedCalls { get; set; }

        public Dictionary<APIFailReason, int> FailReasons { get; set; }
    }

    public enum APIFailReason
    {
        MissingData,
        FailedLookup,
        Other
    }
}