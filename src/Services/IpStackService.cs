using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TemperatureHistogramChallenge.Models;
using IpStack;

namespace TemperatureHistogramChallenge.Services
{

    public class IpStackService : ILocationService
    {
        private readonly IConnectionMultiplexer redis;
        private readonly IApiStats apiStats;
        private readonly ILogger<IpStackService> logger;
        IpStackClient client;

        public IpStackService(IConfiguration configuration, IConnectionMultiplexer redis, IApiStats apiStats, ILogger<IpStackService> logger)
        {
            this.redis = redis;
            this.apiStats = apiStats;
            this.logger = logger;
            client = new IpStackClient(configuration["ipStackApiKey"]);
        }

        public async Task<string> Run(string ip)
        {
            string result = "";

            IDatabase cache = redis.GetDatabase();

            var cachedResult = await CacheLookup($"ips_{ip}", cache);

            if (string.IsNullOrEmpty(cachedResult) || cachedResult == "-1")
            {
                result = GetResource(ip);
                await PutToCache($"ips_{ip}", result, cache);
            }
            else
            {
                logger.LogDebug("IPStack cache hit");
                result = cachedResult;
            }

            return result;
        }

        private async Task PutToCache(string key, string value, IDatabase cache)
        {
            try
            {
                if (!string.IsNullOrEmpty(value))
                    await cache.StringSetAsync(key, value, TimeSpan.FromDays(1));
            }
            catch (RedisException ex)
            {
                logger.LogError(ex, "Redis exception: ");
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
                logger.LogError(ex, "Redis exception: ");
            }

            return cachedResult;
        }

        public string GetResource(string ip)
        {
            try
            {
                var location = client.GetIpAddressDetails(ip);
                var result = string.Join(',', location.Latitude.ToString(), location.Longitude.ToString());
                if (string.IsNullOrEmpty(result) || result == "0,0")
                {
                    throw new Exception("IpStack API Lookup Failed");
                }
                return result;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error in IpStackService: ");
                if (e.InnerException?.InnerException?.Message == "Device not configured")
                    apiStats.Add(ApiFailReason.ConnectionError);
                throw;
            }
        }
    }
}


