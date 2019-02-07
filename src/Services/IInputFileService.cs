using System.Collections.Generic;

namespace TemperatureHistogramChallenge.Services
{
    public interface IInputFileService
    {
        IDictionary<float, int> ProcessFile(IInputFile input);
    }
}