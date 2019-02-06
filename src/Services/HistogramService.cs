using System;
using System.Linq;
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
            try
            {
                var temperatureData = inputFileService.ProcessFile(new InputFile() { FullFilename = input});

                logger.LogDebug($"Total T {temperatureData.Values.Sum()}, unique T: {temperatureData.Count}");

                var histogram = temperatureData.Bucketize(buckets);

                logger.LogInformation(string.Join(Environment.NewLine, apiStats.Summary()));

                if (histogram.Count > 0)
                {
                    outputFileService.SaveFile(histogram, output);
                    logger.LogInformation("Done!");
                }
                else
                {
                    logger.LogWarning("Nothing to save. Check input file, quality of data and connectivity to the Internet.");
                }
            } catch (Exception ex)
            {
                logger.LogError(ex, "Exception: ");
            }

#if DEBUG
            Console.ReadLine();
#endif
        }


    }
}