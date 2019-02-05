using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TemperatureHistogramChallenge.Models
{
    public class ApiStats : IApiStats
    {

        public ConcurrentDictionary<ApiFailReason, int> FailReasons;

        public ApiStats()
        {
            FailReasons = new ConcurrentDictionary<ApiFailReason, int>();
        }
        public void Add(ApiFailReason apiFailReason)
        {
            FailReasons.AddOrUpdate(apiFailReason, 1,(ApiFailReason _, int v) => v + 1);
        }

        public List<string> Summary()
        {
            var total = FailReasons.Values.Sum();

            return (total > 0)
                ? FailReasons.Select(kv => $"{kv.Key.ToString()}: {kv.Value} ({(100.0 * kv.Value / total):F2}%)").ToList()
                : new List<string>();
        }
    }

    public enum ApiFailReason
    {
        MissingData,
        FailedApiLookup,
        ConnectionError
    }
}