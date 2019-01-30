﻿using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TemperatureHistogramChallenge.Services;

namespace TemperatureHistogramChallenge
{
    class Program
    {
        public static void Main(string[] args)
        { 
                #region DependencyInjection

                // create a new ServiceCollection 
                var serviceCollection = new ServiceCollection();

                ConfigureServices(serviceCollection);

                // create a new ServiceProvider
                var serviceProvider = serviceCollection.BuildServiceProvider();

                // logger
                var logger = serviceProvider.GetService<ILogger<Program>>();

                #endregion

                #region CommandLineTools
            try
            {

                var app = new CommandLineApplication();

                app.HelpOption();

                var optionInput = app.Option("-i|--input <INPUT>", "Required. Input temperature file",
                        CommandOptionType.SingleValue)
                    .IsRequired()
                    .Accepts(v => v.ExistingFile());

                var optionOutput = app.Option("-o|--output <OUTPUT>", "Output histogram file",
                    CommandOptionType.SingleValue);

                var optionNumBuckets = app.Option<int>("-n|--numOfBuckets <N>",
                        "Number of buckets for a temperature histogram", CommandOptionType.SingleValue)
                    .Accepts(o => o.Range(1, 1000));

                #endregion

                app.OnExecute(() =>
                {
                    var input = Path.GetFullPath(optionInput.Value());

                    var output = optionOutput.HasValue()
                        ? Path.GetFullPath(optionOutput.Value())
                        : Path.Combine(Directory.GetCurrentDirectory(), "histogram.tsv");

                    var numOfBuckets = optionNumBuckets.HasValue() ? optionNumBuckets.ParsedValue : 1;

                    logger.LogInformation($"In: {input}, Out: {output}, Buckets: {numOfBuckets}");

                    // begin processing
                    serviceProvider.GetService<IHistogramService>().Create(input, output, numOfBuckets);
                });

                app.Execute(args);
            }
            catch (Exception generalException)
            {
                logger.LogError(generalException,
                    "An exception happened while running the application.");
            }

#if DEBUG
            Console.ReadLine();
#endif
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            // add loggers           
            serviceCollection.AddLogging(configure => configure.AddConsole())
                .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Information);

            // Build configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .AddEnvironmentVariables()
                .Build();

            // Add access to IConfiguration
            serviceCollection.AddSingleton(configuration);

            // main service
            serviceCollection.AddSingleton<IHistogramService,HistogramService>();

            // Input File Service
            serviceCollection.AddTransient<IInputFileService, MapReduceFileService>();

            // Ip2Location service
            serviceCollection.AddScoped<ILocationService, IpStackService>();

            // Weather forecast service
            serviceCollection.AddScoped<IWeatherService, OpenWeatherMapService>();

            // Output file service
            serviceCollection.AddTransient<IOutputFileService, TsvFileService>();
        }

    }

}
