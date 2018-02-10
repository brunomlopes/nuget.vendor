using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NugetVendor.Resolver;
using NugetVendor.VendorDependenciesReader;
using Shouldly;
using Xunit;

namespace NugetVendor.Tests
{
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
        public async Task CanFetchOnePackageIntoSpecificFolder()
        {
            
            Parse(@"
source proget https://proget.hq.welisten.eu/nuget/ic-public/

proget InnovationCast.Analyzers 1.0.0.12 into other
");

            var e = new ResolveEngine();
            e.Initialize(_parsedVendor);
            var inMemoryLocalBaseFolder = new InMemoryLocalBaseFolder();
            await e.RunAsync(inMemoryLocalBaseFolder);

            var content = await inMemoryLocalBaseFolder.FileContentOrEmptyAsync(
                @"other\vendor.dependency.description.json", new CancellationToken());

            content.ShouldNotBeNullOrWhiteSpace();
            JsonConvert.DeserializeObject<SomethingWithVersion>(content).Version.ShouldBe("1.0.0.12");

            inMemoryLocalBaseFolder.ContainsPath(@"other\InnovationCast.Analyzers.1.0.0.12.nupkg").ShouldBeTrue();
            inMemoryLocalBaseFolder.ContainsPath(@"other\tools\install.ps1").ShouldBeTrue();
            inMemoryLocalBaseFolder.ContainsPath(@"other\_rels\.rels").ShouldBeFalse("Skip internal nuget folders");
            inMemoryLocalBaseFolder.ContainsPath(@"other\[Content_Types].xml").ShouldBeFalse("Skip internal nuget folders");
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
            var reader = new VendorDependenciesReader.VendorDependenciesReader(new StringReader(fileContent));
            _parsedVendor = reader.ReadAsync().Result;
        }
    }
}