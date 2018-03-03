using NugetVendor.VendorDependenciesReader;

namespace NugetVendor.Resolver.Events
{
    public class Decompressing : EngineEvent, IPackageEngineEvent
    {
        public Decompressing(Package package, Source source, string file, int current, int totalCount)
        {
            Package = package;
            Source = source;
            File = file;
            Current = current;
            TotalCount = totalCount;
        }

        public Package Package { get; set; }
        public Source Source { get; set; }
        public string File { get; set; }
        public int Current { get; set; }
        public int TotalCount { get; set; }
    }
}