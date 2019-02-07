using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using TemperatureHistogramChallenge.Extensions;
using TemperatureHistogramChallenge.Models;

namespace TemperatureHistogramChallenge.Services
{
    public class HistogramService : IHistogramService
    {
        private readonly IInputFileService _inputFileService;
        private readonly ILogger _logger;
        private readonly IOutputFileService _outputFileService;
        private readonly IApiStats _apiStats;

        public HistogramService(IInputFileService inputFileService, ILogger<HistogramService> logger, IOutputFileService outputFileService, IApiStats apiStats)
        {
            _inputFileService = inputFileService;
            _logger = logger;
            _outputFileService = outputFileService;
            _apiStats = apiStats;
        }

        public void Create(string input, string output, int buckets)
        {
            try
            {
                var temperatureData = _inputFileService.ProcessFile(new InputFile() { FullFilename = input});

                _logger.LogDebug($"Total T {temperatureData.Values.Sum()}, unique T: {temperatureData.Count}");

                var histogram = temperatureData.Bucketize(buckets);

                _logger.LogInformation(string.Join(Environment.NewLine, _apiStats.Summary()));

                if (histogram.Count > 0)
                {
                    _outputFileService.SaveFile(histogram, output);
                    _logger.LogInformation("Done!");
                }
                else
                {
                    _logger.LogWarning("Nothing to save. Check input file, quality of data and connectivity to the Internet.");
                }
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: ");
            }

        }


    }
}