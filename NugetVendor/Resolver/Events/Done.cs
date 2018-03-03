using NugetVendor.VendorDependenciesReader;

namespace NugetVendor.Resolver.Events
{
    public class Done : EngineEvent, IPackageEngineEvent
    {
        public Done(Package package)
        {
            Package = package;
        }

        public Package Package { get; set; }
    }
}