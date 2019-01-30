using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using TemperatureHistogramChallenge.Models;

namespace TemperatureHistogramChallenge.Services
{
    public class OpenWeatherMapService : IWeatherService
    {
        private static HttpClient _httpClient = new HttpClient();

        private static string _appkey = "";

        public OpenWeatherMapService(IConfiguration configuration)
        {
            _httpClient.BaseAddress = new Uri("http://api.openweathermap.org/data/2.5/forecast");
            _httpClient.Timeout = new TimeSpan(0, 0, 15);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _appkey = configuration["openWeatherMapsApiKey"];
        }
        public async Task<double> WeatherForecast(DateTime date, string location)
        {
            return await GetResource(date, location);
        }

        private async Task<double> GetResource(DateTime date, string location)
        {
            // TODO: add cache
            var locationParam = "?lat=" + location.Split(',')[0] + "&lon=" + location.Split(',')[1];
            //var unixTimestamp = (Int32)(date.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var response = await _httpClient.GetAsync($"{locationParam}&APPID={_appkey}&units=imperial");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var weather = JsonConvert.DeserializeObject<OpenWeatherMapResponseDto>(content);
            // TODO: validate temperature, if null consider failed API lookup, temperature is in Fahrenheit
            return weather.list[0].main.temp_max;
        }
    }
}