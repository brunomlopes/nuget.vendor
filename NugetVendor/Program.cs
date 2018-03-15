using System;
using System.Drawing;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using NugetVendor.Resolver;
using Console = Colorful.Console;

namespace NugetVendor
{
    class Program
    {
        static void Main(string[] args)
        {
            ILoggerFactory loggerFactory = new LoggerFactory();

            #if DEBUG
            loggerFactory.AddFile("log.txt", LogLevel.Trace);
            #endif 

            CommandLineApplication commandLineApplication =
                new CommandLineApplication(throwOnUnexpectedArg: false);
            
            CommandOption folderCommand = commandLineApplication.Option(
                "-f |--folder <folder>",
                "Output folder",
                CommandOptionType.SingleValue);
            CommandOption vendorsCommand = commandLineApplication.Option(
                "--vendors", "File with vendor dependencies.",
                CommandOptionType.SingleValue);

            CommandOption forceRefreshCommand = commandLineApplication.Option(
                "--force", "Force refresh.",
                CommandOptionType.NoValue);

            CommandOption quiet = commandLineApplication.Option(
                "--quiet", "quiet output",
                CommandOptionType.NoValue);

            commandLineApplication.HelpOption("-? | -h | --help");
            commandLineApplication.OnExecute(() =>
            {
                var folder = "local";
                if (folderCommand.HasValue())
                {
                    folder = folderCommand.Value();
                }

                var vendors = "vendors.txt";
                if (vendorsCommand.HasValue())
                {
                    vendors = vendorsCommand.Value();
                }

                var vendorsFullPath = Path.GetFullPath(vendors);
                if (!File.Exists(vendorsFullPath))
                {
                    Console.WriteLine($"No such file '{vendorsFullPath}'", Color.Red);
                    return -1;
                }

                var folderFullPath = Path.GetFullPath(folder);
                if (File.Exists(folderFullPath))
                {
                    Console.WriteLine($"'{folderFullPath}' must be a directory and is a file", Color.Red);
                }

                Directory.CreateDirectory(folderFullPath);

                Console.WriteLine($"Taking dependencies from '{vendorsFullPath}' into '{folderFullPath}'", Color.Green);
                using (var input = File.OpenText(vendorsFullPath))
                {
                    var parsedVendors = new VendorDependenciesReader.VendorDependenciesReader(input)
                        .ReadAsync()
                        .Result;
                    parsedVendors.Validate();

                    var engine = new ResolveEngine(loggerFactory.CreateLogger<ResolveEngine>()).Initialize(parsedVendors);

                    if (forceRefreshCommand.HasValue())
                    {
                        Console.WriteLine($"Forcing a refresh", Color.Yellow);

                        engine.ForceRefresh();
                    }

                    RenderOutputFromEvents output = null;
                    if(!quiet.HasValue())
                    {
                        output = new RenderOutputFromEvents(parsedVendors, loggerFactory.CreateLogger<RenderOutputFromEvents>());
                        engine.Listen(output.ResolveEngineEventListener);
                    }

                    engine.RunAsync(new LocalBaseFolder(folderFullPath)).Wait();
                    output?.UiTask.Wait(TimeSpan.FromSeconds(1));
                }

                return 0;
            });
            commandLineApplication.Execute(args);
        }
    }
}
