using Xunit;
using TemperatureHistogramChallenge.Models;

namespace CreateWeatherHistogram.Tests
{
    public class ApiStatsSummary
    {
        private readonly ApiStats _apiStats;

        public ApiStatsSummary()
        {
            _apiStats = new ApiStats();
        }

        [Theory]
        [InlineData(ApiFailReason.MissingData)]
        [InlineData(ApiFailReason.Other)]
        [InlineData(ApiFailReason.FailedLookup)]
        public void WhenAddSingleReason_SummaryReturnsOneLine(ApiFailReason reason)
        {
            //arrange
            _apiStats.Add(reason);

            //act
            var result = _apiStats.Summary();

            //assert
            Assert.Single(result);
        }

        [Fact]
        public void WhenAddThreeSameReasons_SummaryReturnsOneLine()
        {
            //arrange
            var reason = ApiFailReason.Other;
            _apiStats.Add(reason);
            _apiStats.Add(reason);
            _apiStats.Add(reason);

            //act
            var result = _apiStats.Summary();

            //assert
            Assert.Single(result);
            Assert.Contains("Other: 3 (100.0%)", result[0]);
        }

        [Fact]
        public void WhenAddEachReasonOnce_SummaryReturnsTreeLines()
        {
            //arrange
            _apiStats.Add(ApiFailReason.MissingData);
            _apiStats.Add(ApiFailReason.Other);
            _apiStats.Add(ApiFailReason.FailedLookup);

            //act
            var result = _apiStats.Summary();

            //assert
            Assert.Equal(3, result.Count);
            Assert.Contains("MissingData: 1 (33.0%)", result[0]);
            Assert.Contains("FailedLookup: 1 (33.0%)", result[1]);
            Assert.Contains("Other: 1 (33.0%)", result[2]);
        }
    }
}
