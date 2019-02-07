
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace TemperatureHistogramChallenge.Models
{
    public enum ApiFailReason
    {
        MissingData,
        FailedLookup,
        Other
    }

    public class ApiStats : IApiStats
    {
        private ConcurrentDictionary<ApiFailReason, int> _failReasons;

        public int TotalCalls { get { return _failReasons.Sum(x => x.Value); } }

        public ApiStats()
        {
            _failReasons = new ConcurrentDictionary<ApiFailReason, int>(System.Environment.ProcessorCount * 2, 10);
        }

        public void Add(ApiFailReason apiFailReason)
        {
            // Add apiFailReason with value of 1 if it does NOT exist. Otherwise, add one to its value.
            _failReasons.AddOrUpdate(apiFailReason, 1, (key, value) => value + 1);
        }

        public int CountByReason(ApiFailReason apiFailReason)
        {
            return _failReasons.TryGetValue(apiFailReason, out int i) ? i : 0;
        }

        public List<string> Summary()
        {
            return (TotalCalls > 0)
                ? _failReasons.Select(kv => $"{kv.Key.ToString()}: {kv.Value} ({(100 * kv.Value / TotalCalls):F1}%)")
                .ToList()
                : new List<string>();
        }
    }
}