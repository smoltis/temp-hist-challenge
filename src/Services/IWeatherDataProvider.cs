using System.Threading.Tasks;

namespace TemperatureHistogramChallenge.Services
{
    public interface IWeatherDataProvider
    {
        Task<string> GetAsync(string sourceLocation);
    }
}
