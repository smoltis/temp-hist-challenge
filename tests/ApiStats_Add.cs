using System;
using System.Collections.Generic;
using System.Linq;
using TemperatureHistogramChallenge.Models;
using Xunit;

namespace CreateWeatherHistogram.Tests
{
    public class ApiStats_Add
    {
        private readonly ApiStats _apiStats;
        public ApiStats_Add()
        {
            _apiStats = new ApiStats();
        }
        [Fact]
        public void ApiStats_AddToDictionary1()
        {
            _apiStats.Add(ApiFailReason.MissingData);
            // Assert
            Assert.Single(_apiStats.FailReasons);
        }
        [Fact]
        public void ApiStats_SummaryOutput1()
        {
            //Arrannge
            var expected = new List<string> 
            { 
                "MissingData: 3 (75.00%)",
                "ConnectionError: 1 (25.00%)" 
            };
            _apiStats.Add(ApiFailReason.MissingData);
            _apiStats.Add(ApiFailReason.MissingData);
            _apiStats.Add(ApiFailReason.MissingData);
            _apiStats.Add(ApiFailReason.ConnectionError);
            // Act
            var actual = _apiStats.Summary();
            // Assert
            for(int i=0; i<2; i++)
                Assert.Equal(expected[i], actual[i]);
        }
    }
}
