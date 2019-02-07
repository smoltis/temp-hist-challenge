using Xunit;
using TemperatureHistogramChallenge.Models;

namespace CreateWeatherHistogram.Tests
{
    public class ApiStatsCountByReason
    {
        private readonly ApiStats _apiStats;

        public ApiStatsCountByReason()
        {
            _apiStats = new ApiStats();
        }

        [Theory]
        [InlineData(ApiFailReason.MissingData)]
        [InlineData(ApiFailReason.Other)]
        [InlineData(ApiFailReason.FailedLookup)]
        public void WhenAddSingleReason_ReturnShouldBeOne(ApiFailReason reason)
        {
            //arrange
            //act
            _apiStats.Add(reason);

            //assert
            Assert.Equal(1, _apiStats.CountByReason(reason));
        }

        [Fact]
        public void WhenAddThreeSameReasons_ReturnShouldBeTree()
        {
            //arrange
            var reason = ApiFailReason.Other;

            //act
            _apiStats.Add(reason);
            _apiStats.Add(reason);
            _apiStats.Add(reason);

            //assert
            Assert.Equal(3, _apiStats.CountByReason(reason));
        }

        [Fact]
        public void WhenAddEachReasonOnce_ReturnShouldBeOneForEachReason()
        {
            //arrange
            //act
            _apiStats.Add(ApiFailReason.MissingData);
            _apiStats.Add(ApiFailReason.Other);
            _apiStats.Add(ApiFailReason.FailedLookup);

            //assert
            Assert.Equal(1, _apiStats.CountByReason(ApiFailReason.MissingData));
            Assert.Equal(1, _apiStats.CountByReason(ApiFailReason.Other));
            Assert.Equal(1, _apiStats.CountByReason(ApiFailReason.FailedLookup));
        }
    }
}
