using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using CommandLine;
using CommandLine.Text;
using Serilog;

namespace DirSniper
{
    class Program
    {
        public static Snipe? snipe;

        static async Task Main(string[] args)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            // Parse arguments passed
            Parser parser = new Parser(with =>
            {
                with.CaseInsensitiveEnumValues = true;
                with.CaseSensitive = false;
                with.HelpWriter = null;
            });

            ParserResult<Options> parserResult = parser.ParseArguments<Options>(args);
            parserResult.WithParsed<Options>(o => { Options.Instance = o; })
                .WithNotParsed(errs => DisplayHelp(parserResult, errs));
            Options options = Options.Instance;
            await RunOptionsAndReturnExitCode(options);

            watch.Stop();
            Console.WriteLine("Execution time: " + watch.ElapsedMilliseconds / 1000 + " Seconds");
        }


        private static async Task RunOptionsAndReturnExitCode(Options opts)
        {
            snipe = new Snipe
            {
                Directories = LoadDirectoriesFromResource("DirSniper.directories.txt")
            };
            string outputFileName = snipe.Output ?? DateTime.Now.ToString("yyyyMMddHHmmss") + ".log";

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console();

            if (!string.IsNullOrEmpty(snipe.Output))
            {
                //loggerConfig = loggerConfig.WriteTo.File(outputFileName);
            }

            Log.Logger = loggerConfig.CreateLogger();

            Log.Logger.Information("Starting Directory Brute Force!");
            Console.WriteLine("Starting Directory Brute Force!...");

            await StartBruteForce(snipe.Url, snipe.Directories);
        }


        private static List<string> LoadDirectoriesFromResource(string resourceName)
        {
            var directories = new List<string>();
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Log.Logger.Error("Error loading resource: {ResourceName}", resourceName);
                    return directories;
                }

                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        directories.Add(line);
                    }
                }
            }

            return directories;
        }

        private static async Task StartBruteForce(string baseUrl, List<string> directories)
        {
            using var client = new HttpClient();
            var rootUri = new Uri(baseUrl);
            int maxDegreeOfParallelism = 5; // Adjust this value as needed
            var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

            var tasks = new List<Task>();

            foreach (var directory in directories)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    var uri = new Uri(rootUri, directory);
                    try
                    {
                        var response = await client.GetAsync(uri);
                        if (response.IsSuccessStatusCode)
                        {
                            Log.Logger.Information("Success: {Url}", uri);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Information("Error crawling {Uri}: {Message}", uri, ex.Message);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
}
public class Options
{
            public static Options Instance { get; set; }

// Command line options
            [Option('q', "quiet", Required = false, HelpText = "Do not log anything to the screen")]
            public bool Quiet { get; set; }

            [Option('u', "url", Required = true, HelpText = "Specify a URL to scrape words from")]
            public string Url { get; set; }

            [Option('o', "output", Required = false, HelpText = "Specify a file to output the wordlist to",
Default = null)]
            public string Output { get; set; }

            [Option('t', "threads", Required = false, HelpText = "Specify the number of concurrent threads",
Default = 10)]
            public int Threads { get; set; }

            [Option("delay", Required = false, HelpText = "Specify a delay between requests", Default = 100)]
            public int Delay { get; set; }

            [Option("timeout", Required = false, HelpText = "Specify a timeout for each request", Default = 15)]
            public int Timeout { get; set; }

            [Option('a', "agent", Required = false, HelpText = "Specify a User Agent to use",
                Default =
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36")]
            public string Agent { get; set; }
        }

        private static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            HelpText helpText = HelpText.AutoBuild(result, h =>
            {
                h.AdditionalNewLineAfterOption = false;
                h.Heading = "DirSniper C# Version 0.1"; //change header
                h.Copyright = ""; //change copyright text
                h.AutoVersion = false;
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            Console.WriteLine(helpText);
            System.Environment.Exit(1);
        }
    }
}