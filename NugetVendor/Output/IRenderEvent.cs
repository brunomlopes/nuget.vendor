using NugetVendor.Resolver;

namespace NugetVendor.Output
{
    internal interface IRenderEvent
    {
        void Render(EngineEvent evt);
    }
}