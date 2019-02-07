using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using TemperatureHistogramChallenge.Models;

namespace TemperatureHistogramChallenge.Services
{
    public class OpenWeatherMapService : IWeatherService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<OpenWeatherMapService> _logger;

        private readonly string _tempScale;
        private readonly string _apiKey;

        public OpenWeatherMapService(
            IConfiguration configuration, IConnectionMultiplexer redis,
            ILogger<OpenWeatherMapService> logger)
        {
            _redis = redis;
            _logger = logger;
            _tempScale = configuration["tempScale"];
            _apiKey = configuration["openWeatherMapsApiKey"];
        }

        public async Task<float> WeatherForecast(string location)
        {
            float result;

            IDatabase cache = _redis.GetDatabase();

            var cachedResult = await CacheLookup($"owm_{_tempScale}_{location}", cache);

            if (string.IsNullOrEmpty(cachedResult))
            {
                result = await GetResource(location);

                // delay of 2 seconds to keep within free tier limit 60 req/min
                await Task.Delay(2000);

                await PutToCache($"owm_{_tempScale}_{location}", result, cache);
            }
            else
            {
                _logger.LogDebug("OpenWeatherMap cache hit");
                result = float.Parse(cachedResult, CultureInfo.InvariantCulture.NumberFormat);
            }

            return result;
        }

        private async Task PutToCache(string key, float value, IDatabase cache)
        {
            try
            {
                TimeSpan untilMidnight = DateTime.Today.AddDays(1) - DateTime.Now;
                await cache.StringSetAsync(key, value, untilMidnight);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis exception: ");
            }
        }

        private async Task<string> CacheLookup(string key, IDatabase cache)
        {
            string cachedResult = string.Empty;

            try
            {
                cachedResult = await cache.StringGetAsync(key);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(ex, "Redis exception: ");
            }

            return cachedResult;
        }

        private async Task<float> GetResource(string location)
        {
            var retValue = float.NaN;

            IWeatherDataProvider dataProvider = new WeatherDataProvider();

            try
            {
                string req = CreateRequest(location);
                var content = await dataProvider.GetAsync(req);

                var weather = JsonConvert.DeserializeObject<OpenWeatherMapResponseDto>(content);

                if (weather == null)
                {
                    _logger.LogWarning($"Weather deserialization failed, content: {content}");
                    throw new ApplicationException("OpenWeatherMapService unrecognized location coordinates");
                }

                // make sure the weather forecast is for tomorrow
                int unixTimestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                var timestamp = weather.list.FirstOrDefault(_ => _.dt >= unixTimestamp);

                if (timestamp is null)
                {
                    throw new ApplicationException("OpenWeatherMapService forecast not found");
                }

                retValue = timestamp.main.temp;
            }
            catch (FeedException e)
            {
                _logger.LogError(e, "Error in OpenEatherMapService: ");
                throw;
            }
            catch(JsonSerializationException e)
            {
                _logger.LogError($"Deserialization Error: {e.Message}");
            }

            return retValue;
        }

        private string CreateRequest(string location)
        {
            var geoCoords = location.Split(',');

            if (geoCoords.Length != 2)
            {
                throw new ArgumentException(nameof(location), "OpenWeatherMapService invalid location format");
            }

            var locationParam = "http://api.openweathermap.org/data/2.5/forecast";
            locationParam += $"?lat={geoCoords[0]}&lon={geoCoords[1]}";
            var req = $"{locationParam}&APPID={_apiKey}&units={_tempScale}";
            return req;
        }
    }
}