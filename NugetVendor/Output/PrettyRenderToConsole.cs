using System;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using NugetVendor.Resolver;
using NugetVendor.Resolver.Events;
using NugetVendor.VendorDependenciesReader;
using Console = Colorful.Console;

namespace NugetVendor.Output
{
    internal class PrettyRenderToConsole : IRenderEvent
    {
        private (int left, int top) _start;
        private readonly ImmutableDictionary<Package, int> _linePerPackage;
        
        private const int TopMarginPerPackage = 1;
        private readonly int _packageIdMaxLength = 0;
        public PrettyRenderToConsole(ParsedVendorDependencies deps)
        {
            _start = (Console.CursorLeft, Console.CursorTop);
            _linePerPackage = deps.Packages.Select((p,i) => (p,i)).ToImmutableDictionary(t => t.Item1, t => t.Item2+TopMarginPerPackage);

            _packageIdMaxLength = deps.Packages.Select(p => GetPackagePrefix(p).Length).Max() + 1;

            
            foreach(var package in _linePerPackage.Keys)
            {
                var pos = _start;
                pos.top += _linePerPackage[package];
                SetCursorPosition(0, pos.top);
                Console.WriteLine(GetPackagePrefix(package), Color.Gray);
            }
        }

        public void Render(EngineEvent evt)
        {
            var pos = _start;
            if (evt is IPackageEngineEvent packageEvt)
            {
                pos.top += _linePerPackage[packageEvt.Package];
                Console.ResetColor();
                SetCursorPosition(0, pos.top);
                    
                Console.Write(GetPackagePrefix(packageEvt.Package), Color.Gray);
                SetCursorPosition(_packageIdMaxLength, pos.top);
                Console.ResetColor();

                switch (packageEvt)
                {
                    case AlreadyUpToDate upToDate:
                        Console.Write("Up to date                                 ", Color.Green);
                        break;
                    case Done done:
                        Console.Write("Done                                       ", Color.Green);
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
                        SetCursorPosition(Math.Min(pos.left, Console.BufferWidth), Math.Min(pos.top, Console.BufferHeight));
                        Console.WriteLine("\tAll done", Color.Green);

                        Console.ResetColor();
                        Console.ReplaceAllColorsWithDefaults();
                        break;
                }
                SetCursorPosition(0, _start.top+_linePerPackage.Values.Max());
            }
        }

        
        private void SetCursorPosition(int left, int top)
        {
            Console.SetCursorPosition(Math.Min(left, Console.BufferWidth), Math.Min(top, Console.BufferHeight));
        }

        private static string GetPackagePrefix(Package package)
        {
            return $"{package.PackageId} ({package.PackageVersion}) : ";
        }
    }
}