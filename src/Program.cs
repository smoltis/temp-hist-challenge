using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TemperatureHistogramChallenge.Models;
using TemperatureHistogramChallenge.Services;

namespace TemperatureHistogramChallenge
{
    class Program
    {
        public static void Main(string[] args)
        {
            var serviceProvider = GetServiceProvider();

            var logger = serviceProvider.GetService<ILogger<Program>>();
            var app = new CommandLineApplication {Name = "CreateWeatherHistogram"};
            app.HelpOption();

            ConfigureCliParameters(app, out var optionInput, out var optionOutput, out var optionNumBuckets);

            app.OnExecute(() =>
            {
                ExecutionCallback(serviceProvider, logger, optionInput, optionOutput, optionNumBuckets);
            });

            app.Execute(args);
            Console.ReadLine();
        }

        private static void ExecutionCallback(IServiceProvider serviceProvider, ILogger logger, CommandOption input, CommandOption output, CommandOption<int> numBuckets)
        {
            var inputFileFullPath = Path.GetFullPath(input.Value());

            var outputFileFullPath = output.HasValue()
                ? Path.GetFullPath(output.Value())
                : Path.Combine(Directory.GetCurrentDirectory(), "histogram.tsv");

            var buckets = numBuckets.HasValue() ? numBuckets.ParsedValue : 1;

            logger.LogInformation($"Input: {inputFileFullPath}{Environment.NewLine}Out: {outputFileFullPath}{Environment.NewLine}Buckets: {buckets}");

            serviceProvider.GetService<IHistogramService>().Create(inputFileFullPath, outputFileFullPath, buckets);
        }

        private static void ConfigureCliParameters(CommandLineApplication app, out CommandOption input, out CommandOption output, out CommandOption<int> numBuckets)
        {
            input = app.Option("-i|--input <INPUT>", "Required. Input temperature file",
                    CommandOptionType.SingleValue)
                .IsRequired()
                .Accepts(v => v.ExistingFile());
            output = app.Option("-o|--output <OUTPUT>", "Output histogram file name. Default: ./histogram.tsv",
                CommandOptionType.SingleValue);
            numBuckets = app.Option<int>("-n|--numOfBuckets <N>",
                    "Number of buckets for a temperature histogram", CommandOptionType.SingleValue)
                .Accepts(o => o.Range(1, 1000));
        }

        private static ServiceProvider GetServiceProvider()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            return serviceProvider;
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging(configure => configure.AddConsole())
                .Configure<LoggerFilterOptions>(options =>
                    options.MinLevel = (Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT") == "Development") 
                        ? LogLevel.Trace 
                        : LogLevel.Information);

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .AddEnvironmentVariables()
                .Build();

            serviceCollection.AddSingleton(configuration);

            serviceCollection.AddSingleton<IHistogramService,HistogramService>();

            serviceCollection.AddTransient<IInputFileService, MapReduceFileService>();

            serviceCollection.AddScoped<ILocationService, IpStackService>();

            serviceCollection.AddScoped<IWeatherService, OpenWeatherMapService>();

            serviceCollection.AddTransient<IOutputFileService, TsvFileService>();

            var redisConnectionString = (Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT") == "Development")
            ? "localhost"
            : $"{configuration["redisServer"]}:{configuration["redisPort"]}";
            serviceCollection.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

            serviceCollection.AddSingleton<IApiStats, ApiStats>();

        }
    }

}
