using System;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using Microsoft.Extensions.Logging;
using NugetVendor.Resolver;
using NugetVendor.Resolver.Events;
using NugetVendor.VendorDependenciesReader;
using Console = Colorful.Console;

namespace NugetVendor.Output
{
    internal class PrettyRenderToConsole : IRenderEvent
    {
        private readonly ILogger _log;
        private (int left, int top) _start;
        private readonly ImmutableDictionary<Package, int> _linePerPackage;
        
        private const int TopMarginPerPackage = 1;
        private readonly int _packageIdMaxLength = 0;
        public PrettyRenderToConsole(ParsedVendorDependencies deps, ILogger log)
        {
            _log = log;
            _linePerPackage = deps.Packages.Select((p,i) => (package:p,index:i))
                .ToImmutableDictionary(t => t.package, t => t.index+TopMarginPerPackage);

            CreateEnoughLinesInConsoleToWriteAllPackagesPlusSpacing();

            _start = (Console.CursorLeft, Console.CursorTop);

            _packageIdMaxLength = deps.Packages.Select(p => GetPackagePrefix(p).Length).Max() + 1;
            
            foreach(var package in _linePerPackage.Keys)
            {
                var pos = _start;
                pos.top += _linePerPackage[package];
                Console.SetCursorPosition(0, pos.top);
                Console.WriteLine(GetPackagePrefix(package), Color.Gray);
            }
        }

        private void CreateEnoughLinesInConsoleToWriteAllPackagesPlusSpacing()
        {
            // This shifts the buffer low enough that we have lines to work with
            var numberOfLinesToEnsureExist = _linePerPackage.Values.Count() + 2;
            foreach (var _ in Enumerable.Repeat(0, numberOfLinesToEnsureExist))
            {
                Console.WriteLine();
            }
            Console.SetCursorPosition(0, Console.CursorTop - numberOfLinesToEnsureExist);
        }

        public void Render(EngineEvent evt)
        {
            _log.LogTrace("Rendering event {event}", evt.GetType().Name);
            var pos = _start;
            if (evt is IPackageEngineEvent packageEvt)
            {
                pos.top += _linePerPackage[packageEvt.Package];
                Console.ResetColor();
                Console.SetCursorPosition(0, pos.top);

                Console.Write(GetPackagePrefix(packageEvt.Package), Color.Gray);
                Console.SetCursorPosition(_packageIdMaxLength, pos.top);
                Console.ResetColor();
            }
            
            switch (evt)
            {
                case AlreadyUpToDate upToDate:
                    Console.Write("Up to date                                 ", Color.Green);
                    break;
                case Done done:
                    Console.Write("Done                                       ", Color.Green);
                    break;
                case Resolving resolving:
                    Console.Write("Resolving ", Color.Yellow);
                    Console.Write($"(from {resolving.Source.Name})            ", Color.Yellow);
                    break; 
                case Downloading downloading:
                    Console.Write("Downloading ", Color.Yellow);
                    Console.Write($"(from {downloading.Source.Name})          ", Color.Yellow);
                    break;                    
                case Cleaning cleaning:
                    Console.Write($"Cleaning path {cleaning.FolderName}       ", Color.Yellow);
                    break;
                case Downloaded downloaded:
                    Console.Write("Downloaded                                 ", Color.Green);
                    break;
                case Decompressing decompressing:
                    if(decompressing.TotalCount < 100 || decompressing.Current % 100 == 0) 
                        Console.Write($"Decompressing {decompressing.Current+1:D5}/{decompressing.TotalCount:D5}             ",
                            Color.Yellow);
                    break;
                case AllDone allDone:
                    pos.top += _linePerPackage.Values.Max()+1;
                    Console.SetCursorPosition(0, pos.top);
                    Console.WriteLine("\tAll done", Color.Green);

                    Console.ResetColor();
                    Console.ReplaceAllColorsWithDefaults();
                    return;
            }

            Console.SetCursorPosition(0, _start.top+_linePerPackage.Values.Max());
        }

        private static string GetPackagePrefix(Package package)
        {
            return $"{package.PackageId} ({package.PackageVersion}) : ";
        }
    }
}