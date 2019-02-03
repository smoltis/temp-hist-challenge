using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
            IDatabase cache = redis.GetDatabase();
            string cachedResult = await cache.StringGetAsync($"ips_{ip}");  
            if(string.IsNullOrEmpty(cachedResult))
            {
                result = GetResource(ip);
                if (result != null)
                    await cache.StringSetAsync($"ips_{ip}", result, TimeSpan.FromDays(1));  
            }
            else
            {
                logger.LogDebug("From IPStack cache");
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
                /*
                var response = await _httpClient.GetAsync($"{ip}?access_key={_appkey}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                // TODO: try to deserialize only tags of interest
                var location = JsonConvert.DeserializeObject<IpStackResponseDto>(content);
                // TODO: if location.city is null then consider failed API lookup
                if (location is null)
                {
                    logger.LogWarning($"Location deserialization failed, content: {location}");
                }
                if (location is null)
                {
                    var error = JsonConvert.DeserializeObject<IpStackError>(content);
                    if (error != null && error.error.type == "missing_access_key")
                    {
                        apiStats.Add(ApiFailReason.InvalidAccessKey);
                    }
                    else
                    {
                        logger.LogWarning($"Location API response not recognised: {content}");
                    }
                }
                if (string.IsNullOrEmpty(location.city))
                {
                        // TODO: add missing data to failed API call statistics
                        apiStats.Add(ApiFailReason.FailedLookup);
                    return null;
                }
                */
                
                return string.Join(',', location.Latitude.ToString(), location.Longitude.ToString());
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error in IpStackService: ");
                if (e.Message == "Device not configured")
                    apiStats.Add(ApiFailReason.ConnectionError);
                else
                {
                    logger.LogWarning($"Location http exception not recognised: {e.Message}");
                }
                return null;
            }
            //catch (Exception ex)
            //{
            //    logger.LogError(ex, "Exception: ");
            //    return null;
            //}
        }
    }
}


