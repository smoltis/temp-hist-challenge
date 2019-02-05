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

            var min = Math.Floor(source.First().Key);
            var max = Math.Ceiling(source.Last().Key);


            var bucketSize = (max - min) / totalBuckets;

            for (int i = 0; i < totalBuckets; i++)
            {
                var bucket = new Bucket();
                bucket.bucketMin = (min + i * bucketSize);
                bucket.bucketMax = (bucket.bucketMin + bucketSize);
                bucket.Count = source.Where(kv => (kv.Key >= bucket.bucketMin && kv.Key < bucket.bucketMax)).Sum(kv => kv.Value);
                buckets.Add(bucket);
            }

            return buckets;
        }

    }
}
