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

        public MapReduceFileService(ILogger<MapReduceFileService> logger, ILocationService locationService,IWeatherService weatherService)
        {
            _logger = logger;
            _locationService = locationService;
            _weatherService = weatherService;
        }
        //TODO: resolve locations here to reduce the size of data transfer between classes
        public IDictionary<double, int> ProcessFile(string input)
        {
            //var globalTemperatureData = new Dictionary<int, Tuple<DateTime, string, int>>();
            var globalTemperatureData = new Dictionary<double, int>();

            #region MapReduce

            Parallel.ForEach(File.ReadLines(input),
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                // Initializer:  create task-local storage:
                () => new Dictionary<double, int>(),

                // Loop-body: mapping our results into the local storage
                 (line, _, localTempData) =>
                {
                    var tempLine = ParseLine(line);
                    if (tempLine == null)
                        return localTempData;

                    //TODO: watch out for collisions
                    // 1. resolve IP to location
                    var location = _locationService.Run(tempLine.IP).Result;
                    if (location is null)
                        return localTempData;
                    // 2. get the temperature forecast for tomorrow in the resolved location

                    // TODO: add unsuccessful response handling here
                    var tKey = _weatherService.WeatherForecast(tempLine.Date, location).Result;

                    if (!localTempData.ContainsKey(tKey))
                    {
                        //var val = Tuple.Create(tempLine.Date, tempLine.IP, 1);
                        _logger.LogDebug($"{tempLine.Date}:{tempLine.IP}:{location}:{tKey}");
                        localTempData[tKey] = 1;
                        //_logger.LogDebug(localTempData.TryAdd(tKey, 1) ? "Added" : "Failed");
                    }
                    else
                    {
                        //var newVal = localTempData[tKey].Item3;
                        //var tuple = Tuple.Create(tempLine.Date, tempLine.IP, newVal+1);
                        localTempData[tKey]++;
                    }

                    return localTempData;
                },
                // Finalizer: reduce(merge) individual local storage into global storage
                (localDict) =>
                {
                    //TODO: investigate whether blocking collection can be used
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
            );

            #endregion

            return globalTemperatureData
                .OrderBy(kv => kv.Key)
                .ToDictionary(kv => kv.Key, kv => kv.Value); ;
        }

        public bool ValidateIPv4(string ipString)
        {
            if (ipString.Count(c => c == '.') != 3) return false;
            IPAddress address;
            return IPAddress.TryParse(ipString, out address);
        }

        public TemperatureFileLine ParseLine(string line)
        {
            //TODO: add data cleansing(scrubbing) here
            var columns = line.Split('\t');

            // column count validation
            if (columns.Length < 23)
                return null;

            // date validation
            //TODO: find out TZ for input data
            DateTime dt;
            if (!DateTime.TryParse(columns[0].Substring(0, 10), out dt))
                return null;

            var ipAddr = columns[23].Trim();
            // ip address validation
            if (!ValidateIPv4(ipAddr))
                return null;

            return 
                new TemperatureFileLine()
                {

                    Date = dt.AddDays(1),
                    IP = ipAddr
                };
        }
    }
}