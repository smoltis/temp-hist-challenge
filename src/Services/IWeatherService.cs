using System.Threading.Tasks;

namespace TemperatureHistogramChallenge.Services
{
    public interface IWeatherService
    {
        Task<float> WeatherForecast(string location);
    }
}