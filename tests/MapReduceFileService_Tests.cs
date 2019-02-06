using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TemperatureHistogramChallenge.Models;
using TemperatureHistogramChallenge.Services;
using Xunit;
using FluentAssertions;
using System.IO;
using System.Collections.Generic;

namespace CreateWeatherHistogram.Tests
{
    public class MapReduceFileService_Tests
    {
        [Fact]
        public void MapReduceFileService_TestService()
        {
            // Arrange
            var logger = new Mock<ILogger<MapReduceFileService>>();

            var locationService = new Mock<ILocationService>();
            locationService.Setup(x => x.Run(It.IsAny<string>()))
                .Returns(Task.FromResult("42,42"));

            var weatherService = new Mock<IWeatherService>();
            weatherService.Setup(x => x.WeatherForecast(It.IsAny<string>()))
                .Returns(Task.FromResult(22.5F));

            var apiStats = new Mock<IApiStats>();
            apiStats.Setup(x => x.Add(It.IsAny<ApiFailReason>()));


            var mapReduceFileService = new MapReduceFileService(logger.Object, 
                                                                locationService.Object, 
                                                                weatherService.Object, 
                                                                apiStats.Object);

            string[] fileLine = { "1\t2\t3\t4\t5\t6\t7\t8\t9\t10\t11\t12\t13\t141\t151\t16\t17\t18\t19\t20\t21\t22\t23\t10.20.30.40",
            "1\t2\t3\t4\t5\t6\t7\t8\t9\t10\t11\t12\t13\t141\t151\t16\t17\t18\t19\t20\t21\t22\t23\t10.20.30.40" };

            var fileStub = new Mock<IInputFile>();
            fileStub.Setup(x => x.ReadAllLines(It.IsAny<string>()))
                .Returns(fileLine);
            fileStub.Setup(x => x.FullFilename)
                .Returns("file.csv");

            // Act
            var actualIpValid = mapReduceFileService.ValidateIPv4("127.0.0.1");
            var actualLineIp = mapReduceFileService.ParseLine(fileLine[0]);
            Action actualFileEx = () => mapReduceFileService.ProcessFile(new InputFile() { FullFilename = "fakefile.zip"});
            var actualFileOk = mapReduceFileService.ProcessFile(fileStub.Object);

            // Assert
            Assert.True(actualIpValid);
            Assert.Equal("10.20.30.40", actualLineIp.Ip);
            actualLineIp.Should().BeEquivalentTo(new TemperatureFileLine() { Ip = "10.20.30.40" });
            Assert.Throws<FileNotFoundException>(actualFileEx);
            actualFileOk.Should().BeEquivalentTo(new SortedDictionary<float, int>() { { 22.5F, 2 } });
        }
    }
}
