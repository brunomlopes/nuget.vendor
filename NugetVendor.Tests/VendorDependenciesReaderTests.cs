using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
        public void CanAddcommentsInFrontOfSourcesAndPackages()
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

        private void Parse(string fileContent)
        {
            var reader = new VendorDependenciesReader(new StringReader(fileContent));
            _parsedVendor = reader.ReadAsync().Result;
        }
    }

    public class FetchPackagesTests
    {
        private ParsedVendorDependencies _parsedVendor;

        [Fact]
        public async Task CanFetchOnePackage()
        {
            
            Parse(@"
source proget https://proget.hq.welisten.eu/nuget/ic-public/

proget InnovationCast.Analyzers 1.0.0.12
");

            var e = new ResolveEngine();
            e.Initialize(_parsedVendor);
            var inMemoryLocalBaseFolder = new InMemoryLocalBaseFolder();
            await e.RunAsync(inMemoryLocalBaseFolder);

            var content = await inMemoryLocalBaseFolder.FileContentOrEmptyAsync(
                @"InnovationCast.Analyzers\vendor.dependency.description.json", new CancellationToken());

            content.ShouldNotBeNullOrWhiteSpace();
            JsonConvert.DeserializeObject<SomethingWithVersion>(content).Version.ShouldBe("1.0.0.12");

            inMemoryLocalBaseFolder.ContainsPath(@"InnovationCast.Analyzers\InnovationCast.Analyzers.1.0.0.12.nupkg").ShouldBeTrue();
            inMemoryLocalBaseFolder.ContainsPath(@"InnovationCast.Analyzers\tools\install.ps1").ShouldBeTrue();
            inMemoryLocalBaseFolder.ContainsPath(@"InnovationCast.Analyzers\_rels\.rels").ShouldBeFalse("Skip internal nuget folders");
            inMemoryLocalBaseFolder.ContainsPath(@"InnovationCast.Analyzers\[Content_Types].xml").ShouldBeFalse("Skip internal nuget folders");
        }

        [Fact]
        public async Task FullTest()
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

            var e = new ResolveEngine();
            e.Initialize(_parsedVendor);
            var inMemoryLocalBaseFolder = new InMemoryLocalBaseFolder();
            await e.RunAsync(inMemoryLocalBaseFolder);
        }

        class SomethingWithVersion
        {
            public string Version { get; set; }
        }

        private void Parse(string fileContent)
        {
            var reader = new VendorDependenciesReader(new StringReader(fileContent));
            _parsedVendor = reader.ReadAsync().Result;
        }
    }
}