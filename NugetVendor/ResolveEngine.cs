using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugetVendor
{
    public class ResolveEngine
    {
        private ParsedVendorDependencies _vendorDependencies;
        private readonly List<Lazy<INuGetResourceProvider>> _providers;
        private Dictionary<string, SourceRepository> _sources;
        private Dictionary<string, IEnumerable<PackageIdentity>> _packageIdentitiesBySourceName;
        private readonly SourceCacheContext _sourceCacheContext = new SourceCacheContext();

        public ResolveEngine()
        {
            _providers = Repository
                .Provider
                .GetCoreV3()
                .ToList();
        }

        public void Initialize(ParsedVendorDependencies vendorDependencies)
        {
            _vendorDependencies = vendorDependencies;
            _sources = _vendorDependencies.Sources
                .ToDictionary(s => s.Name, s => new SourceRepository(new PackageSource(s.Url), _providers));

            _packageIdentitiesBySourceName = _vendorDependencies.Packages
                .GroupBy(p => p.SourceName)
                .ToDictionary(g => g.Key,
                    g => g.Select(p => new PackageIdentity(p.PackageId, NuGetVersion.Parse(p.PackageVersion))));
        }

        public class VendorDependencyDescription
        {
            public string Version { get; set; }
        }

        public async Task RunAsync(ILocalBaseFolder localBaseFolder)
        {
            var cancelationToken = new CancellationToken();
            _packageIdentitiesBySourceName
                .AsParallel()
                .ForAll(group =>
                {
                    DownloadVendorsFromSourceName(localBaseFolder, @group, cancelationToken).Wait(cancelationToken);
                });
        }

        private async Task DownloadVendorsFromSourceName(ILocalBaseFolder localBaseFolder, KeyValuePair<string, IEnumerable<PackageIdentity>> @group,
            CancellationToken cancelationToken)
        {
            foreach (var package in @group.Value)
            {
                await DownloadPackage(localBaseFolder, @group, cancelationToken, package);
            }
        }

        private async Task DownloadPackage(ILocalBaseFolder localBaseFolder, 
            KeyValuePair<string, IEnumerable<PackageIdentity>> @group,
            CancellationToken cancelationToken, PackageIdentity package)
        {
            var descriptionPath = $@"{package.Id}\vendor.dependency.description.json";
            VendorDependencyDescription description;

            if (localBaseFolder.ContainsFolder(package.Id))
            {
                var content = await localBaseFolder.FileContentOrEmptyAsync(
                    descriptionPath,
                    cancelationToken);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    try
                    {
                        description = JsonConvert.DeserializeObject<VendorDependencyDescription>(content);
                    }
                    catch (Exception e)
                    {
                        // TODO : log this error
                        description = new VendorDependencyDescription {Version = ""};
                    }

                    if (package.Version.ToFullString() == description.Version)
                    {
                        return;
                    }
                }
            }
            await Download(@group.Key, package, localBaseFolder, cancelationToken);
            
            using (var descriptionStream = localBaseFolder.OpenStreamForWriting(descriptionPath))
            {
                description = new VendorDependencyDescription()
                {
                    Version = package.Version.ToFullString()
                };
                var serialized = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(description, Formatting.Indented));

                await descriptionStream.WriteAsync(serialized, 0, serialized.Length, cancelationToken);
            }
        }

        private async Task Download(string sourceName, PackageIdentity package, ILocalBaseFolder localBaseFolder,
            CancellationToken cancelationToken)
        {
            var sourceRepository = _sources[sourceName];
            
            var remoteV3FindPackageByIdResource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>(cancelationToken);

            using (var stream = localBaseFolder.OpenStreamForWriting($@"{package.Id}\{package}.nupkg"))
            {
                await remoteV3FindPackageByIdResource.CopyNupkgToStreamAsync(package.Id, package.Version,
                    stream,
                    _sourceCacheContext,
                    NullLogger.Instance, new CancellationToken()
                );
            }
        }
    }
}