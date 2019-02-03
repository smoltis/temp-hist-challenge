using Microsoft.Extensions.Logging;
using TemperatureHistogramChallenge.Extensions;
using TemperatureHistogramChallenge.Models;

namespace TemperatureHistogramChallenge.Services
{
    class HistogramService : IHistogramService
    {
        private readonly IInputFileService inputFileService;
        private readonly ILogger logger;
        private readonly IOutputFileService outputFileService;
        private readonly IApiStats apiStats;

        public HistogramService(IInputFileService inputFileService, ILogger<HistogramService> logger, IOutputFileService outputFileService, IApiStats apiStats)
        {
            this.inputFileService = inputFileService;
            this.logger = logger;
            this.outputFileService = outputFileService;
            this.apiStats = apiStats;
        }
        public void Create(string input, string output, int buckets)
        {
            // read raw data from file, ip address
            // enrich with next day forecast 
            // (inline to save memory: resolve IP to location as we go, get temperature by location) 
            var temperatureData = inputFileService.ProcessFile(input);
            logger.LogDebug("Total unique temperatures(C): {0}", temperatureData.Count);

            // create a histogram from collected data
            var histogram = temperatureData.Bucketize(buckets);

            // print api request stats
            // TODO: BUG: find out why dictionary is empty and nothing is printed
            apiStats.Summary().ForEach(_ => logger.LogInformation(_));

            outputFileService.SaveFile(histogram, output);

        }


    }
}