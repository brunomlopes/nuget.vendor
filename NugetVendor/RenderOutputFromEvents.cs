using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NugetVendor.Output;
using NugetVendor.Resolver;
using NugetVendor.Resolver.Events;
using NugetVendor.VendorDependenciesReader;
using Console = Colorful.Console;

namespace NugetVendor
{
    class RenderOutputFromEvents
    {
        private readonly ILogger _log;
        private readonly CancellationToken _uiTaskToken;
        private readonly BlockingCollection<EngineEvent> _queue;
        private readonly CancellationTokenSource _uiTaskTokenSource;
        public Task UiTask { get; }

        public RenderOutputFromEvents(ParsedVendorDependencies deps, ILogger log)
        {
            _log = log;
            var renderer = Console.IsOutputRedirected
                ? (IRenderEvent) new SimpleRender(deps)
                : new PrettyRenderToConsole(deps, log);

            _queue = new BlockingCollection<EngineEvent>();
            _uiTaskTokenSource = new CancellationTokenSource();
            _uiTaskToken = _uiTaskTokenSource.Token;
            UiTask = Task.Run(() =>
            {
                while (!_uiTaskToken.IsCancellationRequested)
                {
                    var evt = _queue.Take(_uiTaskToken);
                    log.LogDebug("Rendering {evt}", evt.GetType());
                    renderer.Render(evt);

                    if (evt is AllDone)
                    {
                        _uiTaskTokenSource.Cancel();
                    }
                }
            });
        }


        public void ResolveEngineEventListener(EngineEvent evt)
        {
            _queue.Add(evt, _uiTaskToken);
        }
    }
}