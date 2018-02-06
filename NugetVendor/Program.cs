using System;
using System.Drawing;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;
using Console = Colorful.Console;

namespace NugetVendor
{
    class Program
    {
        static void Main(string[] args)
        {
            CommandLineApplication commandLineApplication =
                new CommandLineApplication(throwOnUnexpectedArg: false);
            
            CommandOption folderCommand = commandLineApplication.Option(
                "-f |--folder <folder>",
                "Output folder",
                CommandOptionType.SingleValue);
            CommandOption vendorsCommand = commandLineApplication.Option(
                "--vendors", "File with vendor dependencies.",
                CommandOptionType.SingleValue);
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

                Console.WriteLine($"Parsing '{vendorsFullPath}' into '{folderFullPath}'", Color.Green);
                using (var input = File.OpenText(vendorsFullPath))
                {
                    var parsedVendors = new VendorDependenciesReader(input)
                        .ReadAsync()
                        .Result;

                    var engine = new ResolveEngine();
                    engine.Initialize(parsedVendors);
                    engine.RunAsync(new LocalBaseFolder(folderFullPath)).Wait();
                }

                return 0;
            });
            commandLineApplication.Execute(args);
        }
    }
}
