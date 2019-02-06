
namespace TemperatureHistogramChallenge.Services
{
    public interface IInputFile
    {
        string FullFilename { get; set; }

        string[] ReadAllLines(string input);
    }
}
