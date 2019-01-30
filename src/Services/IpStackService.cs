using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using TemperatureHistogramChallenge.Models;

namespace TemperatureHistogramChallenge.Services
{

    public class IpStackService : ILocationService
    {
        private static HttpClient _httpClient = new HttpClient();

        private static string _appkey = "";

        public IpStackService(IConfiguration configuration)
        {
            _httpClient.BaseAddress = new Uri("http://api.ipstack.com/");
            _httpClient.Timeout = new TimeSpan(0, 0, 15);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _appkey = configuration["ipStackApiKey"];
        }

        public async Task<string> Run(string ip)
        {
            return await GetResource(ip);
        }

        public async Task<string> GetResource(string ip)
        {
            //TODO: add cache
            var response = await _httpClient.GetAsync($"{ip}?access_key={_appkey}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            // TODO: try to deserialize only tags of interest
            var location = JsonConvert.DeserializeObject<IpStackResponseDto>(content);
            // TODO: if location.city is null then consider failed API lookup
            if (string.IsNullOrEmpty(location.city))
            {
                // TODO: add missing data to failed API call statistics
                return null;
            }
            return string.Join(',', location.latitude, location.longitude);
        }
    }
}


