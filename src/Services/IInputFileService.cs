using System.Collections.Generic;
using System.IO;

namespace TemperatureHistogramChallenge.Services
{
    public interface IInputFileService
    {
        IDictionary<float, int> ProcessFile(IInputFile input);
    }
}