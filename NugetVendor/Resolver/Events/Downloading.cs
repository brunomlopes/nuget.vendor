using NugetVendor.VendorDependenciesReader;

namespace NugetVendor.Resolver.Events
{
    public class Downloading : EngineEvent, IPackageEngineEvent
    {
        public Downloading(Package package, Source source)
        {
            Package = package;
            Source = source;
        }

        public Package Package { get; set; }
        public Source Source { get; set; }
    }
}