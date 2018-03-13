using System.IO;
using System.Linq;
using NugetVendor.VendorDependenciesReader;
using Shouldly;
using Xunit;

namespace NugetVendor.Tests
{
    public class VendorDependenciesReaderTests
    {
        private ParsedVendorDependencies _parsedVendor;

        [Fact]
        public void CanReadSources()
        {
            Parse(@"
source proget https://proget.hq.welisten.eu/nuget/ic-public/
");

            _parsedVendor.Sources.Select(s => s.Name).ShouldContain(name => name == "proget");
            _parsedVendor.Sources.Select(s => s.Url).ShouldContain(url => url == "https://proget.hq.welisten.eu/nuget/ic-public/");
        }

        [Fact]
        public void SourceCanHaveDashesInId()
        {
            Parse(@"
source pro-get https://proget.hq.welisten.eu/nuget/ic-public/
");

            _parsedVendor.Sources.Select(s => s.Name).ShouldContain(name => name == "pro-get");
            _parsedVendor.Sources.Select(s => s.Url).ShouldContain(url => url == "https://proget.hq.welisten.eu/nuget/ic-public/");
        }

        [Fact]
        public void CanReadPackage()
        {
            Parse(@"
proget InnovationCast.Analyzers 1.0.0.12
");

            _parsedVendor.Packages.Select(s => s.SourceName).ShouldContain(name => name == "proget");
            _parsedVendor.Packages.Select(s => s.PackageId).ShouldContain(url => url == "InnovationCast.Analyzers");
            _parsedVendor.Packages.Select(s => s.PackageVersion).ShouldContain(version => version == "1.0.0.12");
            _parsedVendor.Packages.Select(s => s.OutputFolder).ShouldContain(version => version == "InnovationCast.Analyzers");
        }

        [Fact]
        public void CanReadPackageIncludeSpecificFolder()
        {
            Parse(@"
nuget RavenDB.Server 3.5.5-patch-35246 into RavenDB-3.5
");

            _parsedVendor.Packages.Length.ShouldBe(1);
            var package = _parsedVendor.Packages.First();
            package.SourceName.ShouldBe("nuget");
            package.PackageId.ShouldBe("RavenDB.Server");
            package.PackageVersion.ShouldBe("3.5.5-patch-35246");
            package.OutputFolder.ShouldBe("RavenDB-3.5");
        }


        [Fact]
        public void PackageVersionCanIncludePreRelease()
        {
            Parse(@"
proget InnovationCast.Analyzers 1.0.0.12-branch-423
");

            _parsedVendor.Packages.Select(s => s.PackageVersion).ShouldContain(version => version == "1.0.0.12-branch-423");
        }


        [Fact]
        public void PackageIdCanContainDash()
        {
            Parse(@"
proget Redis-64 1.0.0.12-branch-423
");

            _parsedVendor.Packages.Select(s => s.PackageId).ShouldContain("Redis-64");
        }


        [Fact]
        public void CanReadSourceAndPackage()
        {
            Parse(@"
source proget https://proget.hq.welisten.eu/nuget/ic-public/

proget InnovationCast.Analyzers 1.0.0.12
");
            
            _parsedVendor.Sources.Select(s => s.Name).ShouldContain(name => name == "proget");
            _parsedVendor.Sources.Select(s => s.Url).ShouldContain(url => url == "https://proget.hq.welisten.eu/nuget/ic-public/");

            _parsedVendor.Packages.Select(s => s.SourceName).ShouldContain(name => name == "proget");
            _parsedVendor.Packages.Select(s => s.PackageId).ShouldContain(url => url == "InnovationCast.Analyzers");
            _parsedVendor.Packages.Select(s => s.PackageVersion).ShouldContain(version => version == "1.0.0.12");
        }


        [Fact]
        public void CanAddCommentsInFrontOfSourcesAndPackages()
        {
            Parse(@"
source proget https://proget.hq.welisten.eu/nuget/ic-public/ # this is a comment

proget InnovationCast.Analyzers 1.0.0.12 # this is another comment
");
            
            _parsedVendor.Sources.Select(s => s.Name).ShouldContain(name => name == "proget");
            _parsedVendor.Sources.Select(s => s.Url).ShouldContain(url => url == "https://proget.hq.welisten.eu/nuget/ic-public/");

            _parsedVendor.Packages.Select(s => s.SourceName).ShouldContain(name => name == "proget");
            _parsedVendor.Packages.Select(s => s.PackageId).ShouldContain(url => url == "InnovationCast.Analyzers");
            _parsedVendor.Packages.Select(s => s.PackageVersion).ShouldContain(version => version == "1.0.0.12");
            
        }


        [Fact]
        public void PackageMustReferenceAnExistingSource()
        {
            Parse(@"
source proget https://proget.hq.welisten.eu/nuget/ic-public/ # this is a comment

non-existing-source InnovationCast.Analyzers 1.0.0.12 # this is another comment
");
            
            var exception = Should.Throw<VendorDependenciesException>(() => _parsedVendor.Validate());
            exception.Message.ShouldContain("non-existing-source");
            exception.Message.ShouldContain("proget");
        }

        [Fact]
        public void UrlCanBeNuget()
        {
            Parse(@"
source proget https://proget.hq.welisten.eu/nuget/ic-public/
source nuget https://api.nuget.org/v3/index.json

proget PostgreSQL.Server 9.3.4.3
proget dbdeploy.net  2.2.0.3
proget nodejs 4.4.2
nuget Npgsql 3.1.7
nuget Redis-64 2.8.4
nuget RavenDB.Server 3.5.5-patch-35246
");
            
            _parsedVendor.Sources.Select(s => s.Name).ShouldContain("nuget");
            _parsedVendor.Sources.Select(s => s.Url).ShouldContain("https://api.nuget.org/v3/index.json");
        }

        [Fact]
        public void CommentsAreIgnored()
        {
            Parse(@"
# ignore me
proget InnovationCast.Analyzers 1.0.0.12
");

            _parsedVendor.Packages.Select(s => s.SourceName).ShouldContain(name => name == "proget");
            _parsedVendor.Packages.Select(s => s.PackageId).ShouldContain(url => url == "InnovationCast.Analyzers");
            _parsedVendor.Packages.Select(s => s.PackageVersion).ShouldContain(version => version == "1.0.0.12");
        }

        [Fact]
        public void CanMarkPackageAsCleanOnUpdate()
        {
            Parse(@"
proget InnovationCast.Analyzers 1.0.0.12 clean
");

            _parsedVendor.Packages.First()?.CleanOnUpdate.ShouldBe(true);
        }

        [Fact]
        public void CanSetOutputFolderAndMarkPackageAsCleanOnUpdate()
        {
            Parse(@"
proget InnovationCast.Analyzers 1.0.0.12 into analyzers clean
");

            _parsedVendor.Packages.First()?.OutputFolder.ShouldBe("analyzers");
            _parsedVendor.Packages.First()?.CleanOnUpdate.ShouldBe(true);
        }
        [Fact]
        public void CanMarkPackageAsCleanOnUpdateAndSetOutputFolder()
        {
            Parse(@"
proget InnovationCast.Analyzers 1.0.0.12 clean into analyzers 
");

            _parsedVendor.Packages.First()?.OutputFolder.ShouldBe("analyzers");
            _parsedVendor.Packages.First()?.CleanOnUpdate.ShouldBe(true);
        }

        [Fact]
        public void ByDefaultPackagesAreNotMarkedToCleanOnUpdate()
        {
            Parse(@"
proget InnovationCast.Analyzers 1.0.0.12 
");

            _parsedVendor.Packages.First()?.CleanOnUpdate.ShouldBe(false);
        }

        private void Parse(string fileContent)
        {
            var reader = new VendorDependenciesReader.VendorDependenciesReader(new StringReader(fileContent));
            _parsedVendor = reader.ReadAsync().Result;
        }
    }
}