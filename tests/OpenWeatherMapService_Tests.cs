using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using TemperatureHistogramChallenge.Models;
using TemperatureHistogramChallenge.Services;
using Xunit;

namespace CreateWeatherHistogram.Tests
{
    public class OpenWeatherMapService_Tests
    {
        [Fact]
        public void OpenWeatherMapService_TestService()
        {
            var logger = new Mock<ILogger<OpenWeatherMapService>>();

            var configuration = new Mock<IConfiguration>();
            configuration.Setup(x => x[It.IsAny<string>()])
                .Returns("myFakeApiKeyString");

            var redis = new Mock<IConnectionMultiplexer>();
            var mockDatabase = new Mock<IDatabase>();
            redis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                        .Returns(mockDatabase.Object);

            var apiStats = new Mock<IApiStats>();
            apiStats.Setup(x => x.Add(It.IsAny<ApiFailReason>()));

            var owmService = new OpenWeatherMapService(configuration.Object, redis.Object, apiStats.Object, logger.Object);

            // Act
            var t = owmService.WeatherForecast("someFakelocation");
            var owmService1 = new OpenWeatherMapService(configuration.Object, redis.Object, apiStats.Object, logger.Object);

            // Assert
            Assert.ThrowsAsync<AggregateException>(() => t);
        }
    }
}
