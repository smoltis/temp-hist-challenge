namespace TemperatureHistogramChallenge.Models
{
    public interface IWeatherStats
    {
        double TemperatureC { get; set; }
        int Count { get; set; }
    }
}