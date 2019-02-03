﻿using System;
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
                return string.Join(',', location.Latitude.ToString(), location.Longitude.ToString());
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error in IpStackService: ");
                if (e.Message == "Device not configured")
                    apiStats.Add(ApiFailReason.ConnectionError);
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


