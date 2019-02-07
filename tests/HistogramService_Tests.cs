using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using TemperatureHistogramChallenge.Models;
using TemperatureHistogramChallenge.Services;

namespace CreateWeatherHistogram.Tests
{
    public class HistogramServiceTests
    {
        [Fact]
        public void HistogramService_TestService()
        {
            // Arrange
            var logger = new Mock<ILogger<HistogramService>>();

            var inputFileService = new Mock<IInputFileService>();
            inputFileService.Setup(x => x.ProcessFile(It.IsAny<IInputFile>()))
                .Returns(new SortedDictionary<float, int>{ { 24F,10}, { 36.5F, 2} });

            var outputFileService = new Mock<IOutputFileService>();
            outputFileService.Setup(x => 
                x.SaveFile(It.IsAny<List<Bucket>>(), It.IsAny<string>()));

            var apiStats = new Mock<IApiStats>();
            apiStats.Setup(x => x.Add(It.IsAny<ApiFailReason>()));

            var histogramService =
                new HistogramService(inputFileService.Object, logger.Object, outputFileService.Object, apiStats.Object);

            // Act
            Action act = () => histogramService.Create("file.in", "file.out", 10);

            // Assert
            act.Should().NotThrow();
        }
    }
}
