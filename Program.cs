using System;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Validation;

namespace temp_hist_challenge
{
    class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication();

            app.HelpOption();

            var optionInput = app.Option("-i|--input <INPUT>", "Required. Input temperature file", CommandOptionType.SingleValue)
                .IsRequired()
                .Accepts(v => v.ExistingFile());

            var optionOutput = app.Option("-o|--output <OUTPUT>", "Output histogram file", CommandOptionType.SingleValue);

            var optionNumBuckets = app.Option<int>("-n|--numOfBuckets <N>", "Number of buckets for a temperature histogram", CommandOptionType.SingleValue)
                .Accepts(o => o.Range(1, 1000));

            app.OnExecute(() =>
            {
                var input = optionInput.Value();

                var output = optionOutput.HasValue()
                    ? optionOutput.Value()
                    : "histogram.tsv";

                var numOfBuckets = optionNumBuckets.HasValue() ? optionNumBuckets.ParsedValue : 1;

                Console.WriteLine($"In: {input}, Out: {output}, Buckets: {numOfBuckets}");

                return 0;
            });

            return app.Execute(args);
        }
    }
}
