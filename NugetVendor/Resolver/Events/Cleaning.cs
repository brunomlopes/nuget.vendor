using System.IO;
using NugetVendor.VendorDependenciesReader;

namespace NugetVendor.Resolver.Events
{
    public class Cleaning : EngineEvent, IPackageEngineEvent
    {
        public Cleaning(Package package, string folderName)
        {
            FolderName = folderName;
            Package = package;
        }

        public Package Package { get; set; }
        public string FolderName { get; }
    }
}