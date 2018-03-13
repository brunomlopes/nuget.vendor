namespace NugetVendor.VendorDependenciesReader
{
    public class Package
    {
        public string SourceName { get; set; }
        public string PackageId { get; set; }
        public string PackageVersion { get; set; }
        public string OutputFolder { get; set; }
        public bool CleanOnUpdate { get; set; } = false;
    }
}