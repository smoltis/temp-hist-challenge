using System.Collections.Generic;

namespace TemperatureHistogramChallenge.Services
{
    public interface IOutputFileService
    {
        void SaveFile<T>(IEnumerable<T> lines, string outFile);
    }
}