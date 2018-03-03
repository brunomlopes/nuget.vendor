using NugetVendor.VendorDependenciesReader;

namespace NugetVendor.Resolver.Events
{
    public class AlreadyUpToDate : EngineEvent, IPackageEngineEvent
    {
        public AlreadyUpToDate(Package package)
        {
            Package = package;
        }

        public Package Package { get; set; }
    }
}