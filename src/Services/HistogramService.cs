using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TemperatureHistogramChallenge.Extensions;
using TemperatureHistogramChallenge.Models;

namespace TemperatureHistogramChallenge.Services
{
    class HistogramService : IHistogramService
    {
        private readonly IInputFileService _inputFileService;
        private readonly ILogger _logger;
        private readonly IOutputFileService outputFileService;
        private readonly IApiStats apiStats;

        public HistogramService(IInputFileService inputFileService, ILogger<HistogramService> logger, IOutputFileService outputFileService, IApiStats apiStats)
        {
            _inputFileService = inputFileService;
            _logger = logger;
            this.outputFileService = outputFileService;
            this.apiStats = apiStats;
        }
        public void Create(string input, string output, int buckets)
        {
            // read raw data from file
            // enrich with next day forecast inline to save memory, resolve IP to location as we go, get temperature  
            var temperatureData = _inputFileService.ProcessFile(input);
            _logger.LogDebug("Total unique lines: {0}", temperatureData.Count);

            //TODO: create a histogram from collected data
            var histogram = temperatureData.Bucketize(buckets);
            // print api request stats
            // TODO: BUG: find out why dictionary is empty
            apiStats.Summary().ForEach(_ => _logger.LogInformation(_));

            //TODO: write histogram file to disk
            outputFileService.SaveFile(histogram, output);

        }


    }
}