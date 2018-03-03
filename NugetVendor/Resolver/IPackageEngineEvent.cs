using NugetVendor.VendorDependenciesReader;

namespace NugetVendor.Resolver
{
    public interface IPackageEngineEvent
    {
        Package Package { get; set; }
    }
}