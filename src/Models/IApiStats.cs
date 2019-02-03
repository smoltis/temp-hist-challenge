using System.Collections.Generic;

namespace TemperatureHistogramChallenge.Models
{
    public interface IApiStats
    {
        void Add(ApiFailReason apiFailReason);
        List<string> Summary();
    }
}