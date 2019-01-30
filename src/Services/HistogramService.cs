using Microsoft.Extensions.Logging;

namespace TemperatureHistogramChallenge.Services
{
    class HistogramService : IHistogramService
    {
        private readonly IInputFileService _inputFileService;
        private readonly ILogger _logger;

        public HistogramService(IInputFileService inputFileService, ILogger<HistogramService> logger)
        {
            _inputFileService = inputFileService;
            _logger = logger;
        }
        public void Create(string input, string output, int buckets)
        {
            // read raw data from file
            //TODO: enrich with next day forecast, resolve IP to location as we go, get temperature  
            var temperatureData = _inputFileService.ProcessFile(input);
            //TODO: create a histogram from collected data

            //TODO: print api request stats

            //TODO: write histogram file to disk
            
            _logger.LogInformation("Total unique lines: {0}", temperatureData.Count);
        }
    }
}