using System.Collections.Generic;
using System.IO;
using CsvHelper;

namespace TemperatureHistogramChallenge.Services
{
    public class TsvFileService : IOutputFileService
    {
        public void SaveFile<T>(IEnumerable<T> lines, string outFile)
        {
            using (var writer = new StreamWriter(outFile))
            using (var csv = new CsvWriter(writer))
            {
                csv.WriteRecords(lines);
            }
        }
    }
}