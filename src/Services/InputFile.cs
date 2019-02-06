using System.IO;

namespace TemperatureHistogramChallenge.Services
{
    public class InputFile : IInputFile
    {
        public string FullFilename { get; set; }

        public string[] ReadAllLines(string input)
        {
            return File.ReadAllLines(input);
        }
    }
}
