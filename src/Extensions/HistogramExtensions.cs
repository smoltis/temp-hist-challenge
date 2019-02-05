using System;
using System.Collections.Generic;
using System.Linq;
using TemperatureHistogramChallenge.Models;

namespace TemperatureHistogramChallenge.Extensions
{
    public static class HistogramExtensions
    {
        public static List<Bucket> Bucketize(this IDictionary<float, int> source, int totalBuckets)
        {
            var buckets = new List<Bucket>();

            if (source.Count == 0)
                return buckets;

            var min = source.First().Key;
            var max = source.Last().Key+0.01;

            var bucketSize = (max - min) / totalBuckets;

            for (int i = 0; i < totalBuckets; i++)
            {
                var t_min = (min + i * bucketSize);
                var t_max = (min + i * bucketSize) + bucketSize;
                var count = source.Where(kv => (kv.Key >= t_min && kv.Key < t_max)).Sum(kv => kv.Value);
                //TODO: boundaries error due to rounding
                buckets.Add(new Bucket
                {
                    bucketMin = t_min.ToString("F1"),
                    bucketMax = t_max.ToString("F1"),
                    Count = count,
                });
            }

            return buckets;
        }
    }
}
