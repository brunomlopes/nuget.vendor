using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Colorful;
using NugetVendor.Resolver;
using NugetVendor.VendorDependenciesReader;

namespace NugetVendor
{
    class RenderOutputFromEvents
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

        public RenderOutputFromEvents(ParsedVendorDependencies deps)
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
                Console.Write(GetPackagePrefix(package), Color.Gray);
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
                    
                Console.Write(GetPackagePrefix(packageEvt.Package), Color.Gray);
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
            Console.WriteLine("\tAll done", Color.Green);

            Console.ResetColor();
            _uiTaskTokenSource.Cancel();
        }

    }
}