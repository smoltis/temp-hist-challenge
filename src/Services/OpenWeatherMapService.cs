using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using TemperatureHistogramChallenge.Models;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace TemperatureHistogramChallenge.Services
{
    public class OpenWeatherMapService : IWeatherService
    {
        private static HttpClient _httpClient = new HttpClient();
        private static string _appkey = "";
        private readonly IConnectionMultiplexer redis;
        private readonly IApiStats apiStats;
        private readonly ILogger<OpenWeatherMapService> logger;

        public OpenWeatherMapService(IConfiguration configuration, IConnectionMultiplexer redis, IApiStats apiStats, ILogger<OpenWeatherMapService> logger)
        {
            _httpClient.BaseAddress = new Uri("http://api.openweathermap.org/data/2.5/forecast");
            _httpClient.Timeout = new TimeSpan(0, 0, 5);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _appkey = configuration["openWeatherMapsApiKey"];
            this.redis = redis;
            this.apiStats = apiStats;
            this.logger = logger;
        }


        public async Task<float> WeatherForecast(string location)
        {
            float result;
            IDatabase cache = redis.GetDatabase();
            string cachedResult = cache.StringGet($"owm_{location}");  
            if(string.IsNullOrEmpty(cachedResult))
            {
                result = await GetResource(location);
                TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
                cache.StringSet($"owm_{location}", result, untilMidnight);
            }
            else
            {
                logger.LogDebug("From OpenWeatherMap cache");
                result = float.Parse(cachedResult, CultureInfo.InvariantCulture.NumberFormat);
            }
            return result;
        }

        private async Task<float> GetResource(string location)
        {
            try
            {
                var locationParam = "?lat=" + location.Split(',')[0] + "&lon=" + location.Split(',')[1];
                var response = await _httpClient.GetAsync($"{locationParam}&APPID={_appkey}&units=imperial");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var weather = JsonConvert.DeserializeObject<OpenWeatherMapResponseDto>(content);
                // TODO: validate temperature, if null consider failed API lookup, temperature is in Fahrenheit
                if (weather == null)
                {
                    logger.LogWarning($"Weather deserialization failed, content: {content}");
                }
                // TODO: make sure the weather forecast is for tomorrow
                return weather.list[0].main.temp_max;
            }
            catch (HttpRequestException e)
            {
                logger.LogError(e, "Error in OpenEatherMapService: ");
                if (e.Message == "Device not configured")
                    apiStats.Add(ApiFailReason.ConnectionError);
                throw;
            }
            //catch (Exception ex)
            //{
            //    logger.LogError(ex, "Exception: ");
            //    throw;
            //}

        }
    }
}