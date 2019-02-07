using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using IpStack;

namespace TemperatureHistogramChallenge.Services
{

    public class IpStackService : ILocationService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<IpStackService> _logger;
        private readonly IpStackClient _client;

        public IpStackService(IConfiguration configuration, IConnectionMultiplexer redis, 
            ILogger<IpStackService> logger)
        {
            _redis = redis;
            _logger = logger;
            _client = new IpStackClient(configuration["ipStackApiKey"]);
        }

        public async Task<string> Run(string ip)
        {
            string result;

            var cache = _redis.GetDatabase();

            var cachedResult = await CacheLookup($"ips_{ip}", cache);

            if (string.IsNullOrEmpty(cachedResult) || cachedResult == "-1")
            {
                result = GetResource(ip);
                await PutToCache($"ips_{ip}", result, cache);
            }
            else
            {
                _logger.LogDebug("IPStack cache hit");
                result = cachedResult;
            }

            return result;
        }

        private async Task PutToCache(string key, string value, IDatabaseAsync cache)
        {
            try
            {
                if (!string.IsNullOrEmpty(value))
                    await cache.StringSetAsync(key, value, TimeSpan.FromDays(1));
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, "Redis exception: ");
            }
        }

        private async Task<string> CacheLookup(string key, IDatabaseAsync cache)
        {
            string cachedResult = "";
            try
            {
                cachedResult = await cache.StringGetAsync(key);
            }
            catch (RedisException ex)
            {
                _logger.LogError(ex, "Redis exception: ");
            }

            return cachedResult;
        }

        public string GetResource(string ip)
        {
            var location = _client.GetIpAddressDetails(ip);
            var result = string.Join(',', location.Latitude.ToString(CultureInfo.InvariantCulture), location.Longitude.ToString(CultureInfo.InvariantCulture));

            if (string.IsNullOrEmpty(result) || result == "0,0")
            {
                throw new Exception("IpStack API Lookup Failed");
            }

            return result;
        }
    }
}


