using System;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using NugetVendor.Output;
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

            CommandOption noColours = commandLineApplication.Option(
                "--no-colours", "Do not use colours in output",
                CommandOptionType.NoValue);


            commandLineApplication.HelpOption("-? | -h | --help");
            commandLineApplication.OnExecute(() =>
            {
                var useSimpleOutput =
                    noColours.HasValue()
                    || Console.IsOutputRedirected
                    || (Environment.GetEnvironmentVariable("nugetvendor.simpleOutput")
                            ?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false);

                var writer = new ConsoleWriteLine(!useSimpleOutput);


                var log = loggerFactory.CreateLogger<RenderOutputFromEvents>();

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
                    writer.WriteLineError($"No such file '{vendorsFullPath}'");
                    return -1;
                }

                var folderFullPath = Path.GetFullPath(folder);
                if (File.Exists(folderFullPath))
                {
                    writer.WriteLineError($"'{folderFullPath}' must be a directory and is a file");
                }

                Directory.CreateDirectory(folderFullPath);

                writer.WriteLineProgress($"Taking dependencies from '{vendorsFullPath}' into '{folderFullPath}'");
                using (var input = File.OpenText(vendorsFullPath))
                {
                    var parsedVendors = new VendorDependenciesReader.VendorDependenciesReader(input)
                        .ReadAsync()
                        .Result;
                    parsedVendors.Validate();

                    var engine =
                        new ResolveEngine(loggerFactory.CreateLogger<ResolveEngine>()).Initialize(parsedVendors);

                    if (forceRefreshCommand.HasValue())
                    {
                        writer.WriteLineWarning("Forcing a refresh");

                        engine.ForceRefresh();
                    }

                    RenderOutputFromEvents output = null;
                    if (!quiet.HasValue())
                    {
                        var renderer = useSimpleOutput
                            ? (IRenderEvent) new SimpleRender(parsedVendors)
                            : new PrettyRenderToConsole(parsedVendors, log);

                        output = new RenderOutputFromEvents(renderer, log);
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