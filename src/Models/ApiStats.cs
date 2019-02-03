using System.Collections.Generic;
using System.Linq;

namespace TemperatureHistogramChallenge.Models
{
    public class ApiStats : IApiStats
    {
        private object Semaphore => new object();

        private int TotalCalls { get; set; }

        private Dictionary<ApiFailReason, int> FailReasons => new Dictionary<ApiFailReason, int>();

        public void Add(ApiFailReason apiFailReason)
        {
            lock (Semaphore)
            {
                if (!FailReasons.ContainsKey(apiFailReason))
                {
                    FailReasons[apiFailReason] = 1;
                }
                else 
                {
                    FailReasons[apiFailReason]++;
                }
                TotalCalls++;
            }
        }

        public List<string> Summary()
        {
            return (TotalCalls > 0)
                ? FailReasons.Select(kv => $"{kv.Key.ToString()}: {kv.Value} ({100 * kv.Value / TotalCalls:2d})").ToList()
                : new List<string>();
        }
    }

    public enum ApiFailReason
    {
        MissingData,
        FailedLookup,
        ConnectionError
    }
}