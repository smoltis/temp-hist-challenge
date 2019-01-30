using System;
using System.Threading.Tasks;

namespace TemperatureHistogramChallenge.Services
{
    public interface IWeatherService
    {
        Task<double> WeatherForecast(DateTime date, string location);
    }
}