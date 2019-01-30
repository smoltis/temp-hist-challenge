using System;
using System.Collections.Generic;

namespace TemperatureHistogramChallenge.Services
{
    public interface IInputFileService
    {
        IDictionary<double, int> ProcessFile(string input);
    }
}