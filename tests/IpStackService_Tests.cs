using System;
using IpStack.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using TemperatureHistogramChallenge.Models;
using TemperatureHistogramChallenge.Services;
using Xunit;

namespace CreateWeatherHistogram.Tests
{
    public class IpStackService_Tests
    {
        [Fact]
        public void IpStackService_TestService()
        {
            // Arrange
            var logger = new Mock<ILogger<IpStackService>>();

            var configuration = new Mock<IConfiguration>();
            configuration.Setup(x => x[It.IsAny<string>()])
                .Returns("myFakeApiKeyString");

            var redis = new Mock<IConnectionMultiplexer>();
            var mockDatabase = new Mock<IDatabase>();
            redis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                        .Returns(mockDatabase.Object);

            var apiStats = new Mock<IApiStats>();
            apiStats.Setup(x => x.Add(It.IsAny<ApiFailReason>()));

            var ipStackService = new IpStackService(configuration.Object, redis.Object, apiStats.Object, logger.Object);

            // Act
            var t = ipStackService.Run("10.20.30.40");

            // Assert
            Assert.ThrowsAsync<AggregateException>( () => t);

        }

    }
}
