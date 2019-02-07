using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using TemperatureHistogramChallenge.Models;
using TemperatureHistogramChallenge.Services;
using Xunit;

namespace CreateWeatherHistogram.Tests
{
    public class OpenWeatherMapServiceTests
    {
        [Fact]
        public async void OpenWeatherMapService_TestService()
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

            var owmService = new OpenWeatherMapService(configuration.Object, redis.Object, logger.Object);

            // Act
            var act = owmService.WeatherForecast("someFakelocation");
            var openWeatherMapService = new OpenWeatherMapService(configuration.Object, redis.Object, logger.Object);

            // Assert
            await Assert.ThrowsAsync<ArgumentException>( () => act);
        }
    }
}
