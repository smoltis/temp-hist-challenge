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
using System.Linq;

namespace TemperatureHistogramChallenge.Services
{
    public class OpenWeatherMapService : IWeatherService
    {
        private static HttpClient httpClient = new HttpClient();
        private static string appkey = "";
        private readonly IConfiguration configuration;
        private readonly IConnectionMultiplexer redis;
        private readonly IApiStats apiStats;
        private readonly ILogger<OpenWeatherMapService> logger;
        private readonly string tempScale;

        public OpenWeatherMapService(IConfiguration configuration, IConnectionMultiplexer redis, IApiStats apiStats, ILogger<OpenWeatherMapService> logger)
        {
            httpClient.BaseAddress = new Uri("http://api.openweathermap.org/data/2.5/forecast");
            httpClient.Timeout = new TimeSpan(0, 0, 5);
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            appkey = configuration["openWeatherMapsApiKey"];
            this.configuration = configuration;
            this.redis = redis;
            this.apiStats = apiStats;
            this.logger = logger;
            this.tempScale = configuration["tempScale"];
        }


        public async Task<float> WeatherForecast(string location)
        {
            float result;

            IDatabase cache = redis.GetDatabase();

            var cachedResult = await CacheLookup($"owm_{tempScale}_{location}", cache);

            if (string.IsNullOrEmpty(cachedResult))
            {
                result = await GetResource(location);
                // delay of 2 seconds to keep within free tier limit 60 req/min
                await Task.Delay(2000);
                await PutToCache($"owm_{tempScale}_{location}", result, cache);
            }
            else
            {
                logger.LogDebug("OpenWeatherMap cache hit");
                result = float.Parse(cachedResult, CultureInfo.InvariantCulture.NumberFormat);
            }
            return result;
        }

        private async Task PutToCache(string key, float value, IDatabase cache)
        {
            try
            {
                TimeSpan untilMidnight = DateTime.Today.AddDays(1.0) - DateTime.Now;
                await cache.StringSetAsync(key, value, untilMidnight);
            }
            catch (RedisException ex)
            {
                logger.LogWarning(ex, "Redis exception: ");
            }
        }

        private async Task<string> CacheLookup(string key, IDatabase cache)
        {
            string cachedResult = "";
            try
            {
                cachedResult = await cache.StringGetAsync(key);
            }
            catch (RedisException ex)
            {
                logger.LogWarning(ex, "Redis exception: ");
            }
            return cachedResult;
        }

        private async Task<float> GetResource(string location)
        {
            try
            {
                var geoCoord = location.Split(',');
                if (geoCoord.Length != 2)
                {
                    throw new Exception("OpenWeatherMapService invalid location format");
                }
                var locationParam = $"?lat={location.Split(',')[0]}&lon={location.Split(',')[1]}";
                var req = $"{locationParam}&APPID={appkey}&units={tempScale}";
                var response = await httpClient.GetAsync(req);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var weather = JsonConvert.DeserializeObject<OpenWeatherMapResponseDto>(content);
                if (weather == null)
                {
                    throw new Exception("OpenWeatherMapService unrecognised location coordinates");
                }
                // make sure the weather forecast is for tomorrow
                Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                var t = weather.list.FirstOrDefault(_ => _.dt >= unixTimestamp);
                if (t is null)
                {
                    throw new Exception("OpenWeatherMapService forecast not found");
                }
                return t.main.temp;

            }
            catch (HttpRequestException e)
            {
                if (e.Message == "Device not configured")
                    apiStats.Add(ApiFailReason.ConnectionError);
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}