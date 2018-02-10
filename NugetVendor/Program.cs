using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using NugetVendor.Resolver;
using NugetVendor.VendorDependenciesReader;
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

            CommandOption forceRefreshCommand = commandLineApplication.Option(
                "--force", "Force refresh.",
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

                Console.WriteLine($"Parsing '{vendorsFullPath}' into '{folderFullPath}'", Color.Green);
                using (var input = File.OpenText(vendorsFullPath))
                {
                    var parsedVendors = new VendorDependenciesReader.VendorDependenciesReader(input)
                        .ReadAsync()
                        .Result;
                    parsedVendors.Validate();

                    var engine = new ResolveEngine().Initialize(parsedVendors);

                    if (forceRefreshCommand.HasValue())
                    {
                        Console.WriteLine($"Forcing a refresh", Color.Yellow);

                        engine.ForceRefresh();
                    }

                    Output output = new Output(parsedVendors);
                    engine.Listen(output.ResolveEngineEventListener);

                    engine.RunAsync(new LocalBaseFolder(folderFullPath)).Wait();
                    output.AllDone();
                }

                return 0;
            });
            commandLineApplication.Execute(args);
        }

        class Output
        {
            private readonly ParsedVendorDependencies _deps;
            private (int left, int top) _start;
            private ImmutableList<Package> _packages;
            private ImmutableDictionary<Package, int> _linePerPackage;
            private CancellationToken _uiTaskToken;
            private BlockingCollection<EngineEvent> _queue;
            private Task _uiTask;
            private CancellationTokenSource _uiTaskTokenSource;


            private const int topMarginPerPackage = 1;
            private int packageIdMaxLength = 0;

            public Output(ParsedVendorDependencies deps)
            {
                _deps = deps;
                _start = (Console.CursorLeft, Console.CursorTop);
                _linePerPackage = deps.Packages.Select((p,i) => (p,i)).ToImmutableDictionary(t => t.Item1, t => t.Item2+topMarginPerPackage);

                packageIdMaxLength = deps.Packages.Select(p => GetPackagePrefix(p).Length).Max() + 1;

                foreach(var package in _linePerPackage.Keys)
                {
                    var pos = _start;
                    pos.top += _linePerPackage[package];
                    Console.SetCursorPosition(0, pos.top);
                    Console.Write(GetPackagePrefix(package), Color.LightGray);
                }

                _queue = new BlockingCollection<EngineEvent>();
                _uiTaskTokenSource = new CancellationTokenSource();
                _uiTaskToken = _uiTaskTokenSource.Token;
                _uiTask = Task.Run(() =>
                {
                    while (!_uiTaskToken.IsCancellationRequested)
                    {
                        var evt = _queue.Take(_uiTaskToken);
                        if (_uiTaskToken.IsCancellationRequested) return;
                        Render(evt);
                    }
                });
                
            }

            private static string GetPackagePrefix(Package package)
            {
                return $"{package.PackageId} ({package.PackageVersion}) : ";
            }

            public void ResolveEngineEventListener(EngineEvent evt)
            {
                _queue.Add(evt);
            }

            private void Render(EngineEvent evt)
            {
                var pos = _start;
                if (evt is IPackageEngineEvent packageEvt)
                {
                    pos.top += _linePerPackage[packageEvt.Package];
                    Console.ResetColor();
                    Console.SetCursorPosition(0, pos.top);
                    
                    Console.Write(GetPackagePrefix(packageEvt.Package), Color.LightGray);
                    Console.SetCursorPosition(packageIdMaxLength, pos.top);
                    Console.ResetColor();

                    switch (packageEvt)
                    {
                        case AlreadyUpToDate upToDate:
                            Console.Write("Up to date", Color.Green);
                            break;
                        case Done done:
                            Console.Write("Done", Color.Green);
                            break;
                        case Downloading downloading:
                            Console.Write("Downloading ", Color.Yellow);
                            Console.Write($"(from {downloading.Source.Name})", Color.Yellow);
                            break;
                        case Downloaded downloaded:
                            Console.Write("Downloaded");
                            break;
                        case Decompressing decompressing:
                            Console.Write($"Decompressing {decompressing.Current:D5}/{decompressing.TotalCount:D5}",
                                Color.Yellow);
                            break;
                    }
                }
            }

            public void AllDone()
            {
                var pos = _start;
                pos.top += _linePerPackage.Values.Max()+1;
                Console.SetCursorPosition(pos.left, pos.top);
                Console.WriteLine("\tAll done", Color.GreenYellow);


                Console.ResetColor();
                _uiTaskTokenSource.Cancel();
            }

        }

    }
}
