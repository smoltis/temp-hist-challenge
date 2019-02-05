using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TemperatureHistogramChallenge.Models;
using IpStack;
using IpStack.Models;

namespace TemperatureHistogramChallenge.Services
{

    public class IpStackService : ILocationService
    {
        private readonly IConnectionMultiplexer redis;
        private readonly IApiStats apiStats;
        private readonly ILogger<IpStackService> logger;

        IpStackClient client;

        //private IConfiguration _configuration;

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
            string cachedResult = "";
            IDatabase cache = redis.GetDatabase();
            try
            {
                cachedResult = await cache.StringGetAsync($"ips_{ip}");
            }
            catch (RedisException ex)
            {
                logger.LogError(ex, "Redis exception: ");
            }
            if (string.IsNullOrEmpty(cachedResult) || cachedResult == "-1")
                {
                    result = GetResource(ip);
                try
                {
                    if (!string.IsNullOrEmpty(result))
                        await cache.StringSetAsync($"ips_{ip}", result, TimeSpan.FromDays(1));
                }
                catch (RedisException ex)
                {
                    logger.LogError(ex, "Redis exception: ");
                }

            }
                else
                {
                    logger.LogDebug("IPStack cache hit");
                    result = cachedResult;
                }

            return result;
        }

        public string GetResource(string ip)
        {
            try
            {
                // Get single IP address with defaults
                IpAddressDetails location = client.GetIpAddressDetails(ip);
                var result = string.Join(',', location.Latitude.ToString(), location.Longitude.ToString());
                if (string.IsNullOrEmpty(result) || result == "0,0")
                {
                    throw new Exception("IpStack API Lookup Failed");
                }
                else
                    return result;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error in IpStackService: ");
                if (e.InnerException?.InnerException?.Message == "Device not configured")
                    apiStats.Add(ApiFailReason.ConnectionError);
                apiStats.Add(ApiFailReason.FailedLocationLookup);
                return string.Empty;
            }
        }
    }
}


