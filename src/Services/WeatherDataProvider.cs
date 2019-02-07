using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace TemperatureHistogramChallenge.Services
{
    public class WeatherDataProvider : IWeatherDataProvider
    {
        private static HttpClient _httpClient;

        public WeatherDataProvider()
        {
            _httpClient = new HttpClient()
            {
                Timeout = new TimeSpan(0, 0, 5)
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> GetAsync(string sourceLocation)
        {
            if (string.IsNullOrEmpty(sourceLocation) || string.IsNullOrWhiteSpace(sourceLocation))
            {
                throw new ArgumentNullException(nameof(sourceLocation));
            }

            HttpResponseMessage response = null;

            try
            {
                response = await _httpClient.GetAsync(sourceLocation);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException requestException)
            {
                var statusCode = response == null ? -1 : (int)response.StatusCode;
                var message = response == null ? requestException.Message : response.ReasonPhrase;

                throw new FeedException(sourceLocation, statusCode, message, requestException);
            }
        }
    }
}
