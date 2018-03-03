using NugetVendor.VendorDependenciesReader;

namespace NugetVendor.Resolver.Events
{
    public class Resolving : EngineEvent, IPackageEngineEvent
    {
        public Resolving(Package package, Source source)
        {
            Package = package;
            Source = source;
        }

        public Package Package { get; set; }
        public Source Source { get; set; }
    }
}