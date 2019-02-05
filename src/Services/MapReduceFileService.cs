using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TemperatureHistogramChallenge.Models;

namespace TemperatureHistogramChallenge.Services
{
    public class MapReduceFileService : IInputFileService
    {
        private readonly ILogger<MapReduceFileService> logger;
        private readonly ILocationService locationService;
        private readonly IWeatherService weatherService;
        private readonly IApiStats apiStats;

        public MapReduceFileService(ILogger<MapReduceFileService> logger, 
            ILocationService locationService, 
            IWeatherService weatherService,
            IApiStats apiStats)
        {
            this.logger = logger;
            this.locationService = locationService;
            this.weatherService = weatherService;
            this.apiStats = apiStats;
        }

        public IDictionary<float, int> ProcessFile(string input)
        {
            var globalTemperatureData = new SortedDictionary<float, int>();

            #region MapReduce
            var lines = File.ReadAllLines(input);
            logger.LogDebug($"Read {lines.LongLength} lines from the file");
            Parallel.ForEach(lines,
                // Set up MaxDOP
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                // Initializer:  create task-local storage:
                () => new Dictionary<float, int>(),
                // Loop-body: mapping results into the local storage
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
                // get locations and weather forecast here to save memory
                logger.LogDebug($"Read from file {tempLine.Ip}");

                var weatherAtLocationTask = locationService.Run(tempLine.Ip)
                    .ContinueWith(t => { return weatherService.WeatherForecast(t.Result).Result; }
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
                    //apiStats.Add(ApiFailReason.FailedLookup);
                    logger.LogError(ex, "Exception: ");

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
            if (columns.Length < 24)
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