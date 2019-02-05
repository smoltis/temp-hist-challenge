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
            var app = new CommandLineApplication();
            app.Name = "CreateWeatherHistogram";
            app.HelpOption();

            CommandOption optionInput, optionOutput;
            CommandOption<int> optionNumBuckets;

            ConfigureCliParameters(app, out optionInput, out optionOutput, out optionNumBuckets);

            app.OnExecute(() =>
            {
                ExecutionCallback(serviceProvider, logger, optionInput, optionOutput, optionNumBuckets);
            });

            app.Execute(args);
        }

        private static void ExecutionCallback(ServiceProvider serviceProvider, ILogger<Program> logger, CommandOption optionInput, CommandOption optionOutput, CommandOption<int> optionNumBuckets)
        {
            var input = Path.GetFullPath(optionInput.Value());

            var output = optionOutput.HasValue()
                ? Path.GetFullPath(optionOutput.Value())
                : Path.Combine(Directory.GetCurrentDirectory(), "histogram.tsv");

            var numOfBuckets = optionNumBuckets.HasValue() ? optionNumBuckets.ParsedValue : 1;

            logger.LogInformation($"Input: {input}{Environment.NewLine}Out: {output}{Environment.NewLine}Buckets: {numOfBuckets}");

            serviceProvider.GetService<IHistogramService>().Create(input, output, numOfBuckets);
        }

        private static void ConfigureCliParameters(CommandLineApplication app, out CommandOption optionInput, out CommandOption optionOutput, out CommandOption<int> optionNumBuckets)
        {
            optionInput = app.Option("-i|--input <INPUT>", "Required. Input temperature file",
                    CommandOptionType.SingleValue)
                .IsRequired()
                .Accepts(v => v.ExistingFile());
            optionOutput = app.Option("-o|--output <OUTPUT>", "Output histogram file name. Default: ./histogram.tsv",
                CommandOptionType.SingleValue);
            optionNumBuckets = app.Option<int>("-n|--numOfBuckets <N>",
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
