using Xunit;
using TemperatureHistogramChallenge.Models;

namespace CreateWeatherHistogram.Tests
{
    public class ApiStatsAdd
    {
        private readonly ApiStats _apiStats;

        public ApiStatsAdd()
        {
            _apiStats = new ApiStats();
        }

        [Fact]
        public void WhenAddSingleReason_StatsShouldContainOneItem()
        {
            //arrange
            var reason = ApiFailReason.MissingData;

            //act
            _apiStats.Add(reason);

            //assert
            Assert.Equal(1, _apiStats.TotalCalls);
        }

        [Fact]
        public void WhenAddThreeSameReasons_StatsShouldContainOneItem()
        {
            //arrange
            var reason = ApiFailReason.Other;

            //act
            _apiStats.Add(reason);
            _apiStats.Add(reason);
            _apiStats.Add(reason);

            //assert
            Assert.Equal(3, _apiStats.TotalCalls);
        }

        [Fact]
        public void WhenAddEachReasonOnce_StatsShouldContainTreeItems()
        {
            //arrange
            //act
            _apiStats.Add(ApiFailReason.MissingData);
            _apiStats.Add(ApiFailReason.Other);
            _apiStats.Add(ApiFailReason.FailedLookup);

            //assert
            Assert.Equal(3, _apiStats.TotalCalls);
        }
    }
}
