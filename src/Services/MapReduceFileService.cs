using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly IApiStats apiStats;

        public MapReduceFileService(ILogger<MapReduceFileService> logger, 
            ILocationService locationService, 
            IWeatherService weatherService,
            IApiStats apiStats)
        {
            _logger = logger;
            _locationService = locationService;
            _weatherService = weatherService;
            this.apiStats = apiStats;
        }

        public IDictionary<float, int> ProcessFile(string input)
        {
            var globalTemperatureData = new SortedDictionary<float, int>();

            #region MapReduce

            Parallel.ForEach(File.ReadLines(input),
                // Set up MaxDOP
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                // Initializer:  create task-local storage:
                () => new Dictionary<float, int>(),
                // Loop-body: mapping our results into the local storage
                 (line, _, localTempData) => { return Map(line, localTempData); },
                // Finalizer: reduce(merge) individual local storage into global storage
                (localDict) => Reduce(localDict, globalTemperatureData)
            );

            #endregion

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

        private Dictionary<float, int> Map(string line, Dictionary<float, int> localTempData)
        {
            var tempLine = ParseLine(line);
            if (tempLine == null)
            {
                apiStats.Add(ApiFailReason.MissingData);
                return localTempData;
            }
            try
            {
                // TODO: do task continuation only when location succeeded
                // get locations and weather forecast here to save memory
                _logger.LogDebug($"Read from file {tempLine.Ip}");



                var weatherAtLocationTask = _locationService.Run(tempLine.Ip)
                    .ContinueWith(t => { return _weatherService.WeatherForecast(t.Result).Result; }
                        ,TaskContinuationOptions.OnlyOnRanToCompletion);

                var tKey = weatherAtLocationTask.Result;

                if (!localTempData.ContainsKey(tKey))
                {
                    localTempData[tKey] = 1;
                }
                else
                {
                    localTempData[tKey]++;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception: ");
                return localTempData;
            }
            return localTempData;
        }

        public bool ValidateIPv4(string ipString)
        {
            if (ipString.Count(c => c == '.') != 3) return false;
            return IPAddress.TryParse(ipString, out var address);
        }

        public TemperatureFileLine ParseLine(string line)
        {
            var columns = line.Split('\t');

            // column count validation
            if (columns.Length < 23)
                return null;

            var ipAddr = columns[23].Trim().Replace(" ",string.Empty);
            // ip address validation
            if (!ValidateIPv4(ipAddr))
                return null;

            return 
                new TemperatureFileLine()
                {
                    Ip = ipAddr
                };
        }
    }
}