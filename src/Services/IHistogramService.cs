using System.IO;

namespace TemperatureHistogramChallenge.Services
{
    internal interface IHistogramService
    {
        void Create(string input, string output, int buckets);
    }
}