using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TemperatureHistogramChallenge.Models;

namespace TemperatureHistogramChallenge.Services
{
    public class MapReduceFileService : IInputFileService
    {
        private readonly ILogger<MapReduceFileService> _logger;
        private readonly ILocationService _locationService;
        private readonly IWeatherService _weatherService;
        private readonly IApiStats _apiStats;

        public MapReduceFileService(ILogger<MapReduceFileService> logger, ILocationService locationService, 
            IWeatherService weatherService, IApiStats apiStats)
        {
            _logger = logger;
            _locationService = locationService;
            _weatherService = weatherService;
            _apiStats = apiStats;
        }

        public IDictionary<float, int> ProcessFile(IInputFile input)
        {
            var globalTemperatureData = new SortedDictionary<float, int>();

            var lines = input.ReadAllLines(input.FullFilename);
            _logger.LogDebug($"Read {lines.LongLength} lines from the file");
            Parallel.ForEach(lines,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                // Initializer:  create task-local storage:
                () => new Dictionary<float, int>(),
                // Loop-body: mapping results into the local storage
                (line, _, localTempData) => Map(line, localTempData).Result,
                // Finalizer: reduce(merge) individual local storage into global storage
                (localDict) => Reduce(localDict, globalTemperatureData)
            );

            return globalTemperatureData;
        }

        private static void Reduce(Dictionary<float, int> localDict, SortedDictionary<float, int> globalTemperatureData)
        {
            lock (globalTemperatureData)
            {
                foreach (var key in localDict.Keys)
                {
                    var value = localDict[key];
                    if (!globalTemperatureData.ContainsKey(key))
                    {
                        globalTemperatureData[key] = value;
                    }
                    else
                    {
                        globalTemperatureData[key] += value;
                    }
                }
            }
        }

        private async Task<Dictionary<float, int>> Map(string line, Dictionary<float, int> localTempData)
        {
            var tempLine = ParseLine(line);
            if (tempLine == null)
            {
                _apiStats.Add(ApiFailReason.MissingData);
                return localTempData;
            }
            try
            {
                // get locations and weather forecast here to save memory
                _logger.LogDebug($"Read from file {tempLine.Ip}");

                var weatherAtLocationTask = await _locationService.Run(tempLine.Ip)
                    .ContinueWith(async t => await _weatherService.WeatherForecast(await t),TaskContinuationOptions.OnlyOnRanToCompletion);

                var tKey = await weatherAtLocationTask;

                if (!localTempData.ContainsKey(tKey))
                {
                    localTempData[tKey] = 1;
                }
                else
                {
                    localTempData[tKey]++;
                }

            }
            catch (FeedException ex)
            {
                _apiStats.Add(ApiFailReason.FailedLookup);
                _logger.LogError(ex, "Exception: ");
            }
            catch (Exception ex)
            {
                _apiStats.Add(ApiFailReason.Other);
                _logger.LogError(ex, "Exception: ");
            }

            return localTempData;
        }

        public bool ValidateIPv4(string ipString)
        {
            return ipString.Count(c => c == '.') == 3 && IPAddress.TryParse(ipString, out _);
        }

        public TemperatureFileLine ParseLine(string line)
        {
            var columns = line.Split('\t');

            if (columns.Length < 24)
                return null;

            var ipAddress = columns[23].Trim().Replace(" ",string.Empty);

            if (!ValidateIPv4(ipAddress))
                return null;

            return 
                new TemperatureFileLine()
                {
                    Ip = ipAddress
                };
        }
    }
}