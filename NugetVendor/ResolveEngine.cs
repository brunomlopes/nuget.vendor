using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
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
        private Dictionary<string, IEnumerable<InternalPackageInformation>> _packageIdentitiesBySourceName;
        private readonly SourceCacheContext _sourceCacheContext = new SourceCacheContext();
        private bool _forceRefresh;

        public ResolveEngine()
        {
            _providers = Repository
                .Provider
                .GetCoreV3()
                .ToList();
        }

        public ResolveEngine ForceRefresh()
        {
            _forceRefresh = true;
            return this;
        }

        class InternalPackageInformation
        {
            public string OutputFolder { get; set; }
            public PackageIdentity Identity { get;set; }
        }
        public void Initialize(ParsedVendorDependencies vendorDependencies)
        {
            _vendorDependencies = vendorDependencies;
            _sources = _vendorDependencies.Sources
                .ToDictionary(s => s.Name, s => new SourceRepository(new PackageSource(s.Url), _providers));

            _packageIdentitiesBySourceName = _vendorDependencies.Packages
                .GroupBy(p => p.SourceName)
                .ToDictionary(g => g.Key,
                    g => g.Select(p => new InternalPackageInformation()
                    {
                        OutputFolder = p.OutputFolder,
                        Identity = new PackageIdentity(p.PackageId, NuGetVersion.Parse(p.PackageVersion))
                    }));
        }

        public class VendorDependencyDescription
        {
            public string Version { get; set; }
        }

        public async Task RunAsync(ILocalBaseFolder localBaseFolder)
        {
            var cancelationToken = new CancellationToken();
            var runningTasks = _packageIdentitiesBySourceName
                .Select(group => DownloadVendorsFromSourceName(localBaseFolder, @group, cancelationToken))
                .ToArray();

            await Task.WhenAll(runningTasks);
        }

        private async Task DownloadVendorsFromSourceName(ILocalBaseFolder localBaseFolder, KeyValuePair<string, IEnumerable<InternalPackageInformation>> @group,
            CancellationToken cancelationToken)
        {
            foreach (var package in @group.Value)
            {
                await DownloadPackage(localBaseFolder, @group, cancelationToken, package);
            }
        }

        private async Task DownloadPackage(ILocalBaseFolder localBaseFolder, 
            KeyValuePair<string, IEnumerable<InternalPackageInformation>> @group,
            CancellationToken cancelationToken, InternalPackageInformation info)
        {
            
            var descriptionPath = $@"{info.OutputFolder}\vendor.dependency.description.json";
            VendorDependencyDescription description;

            if (!_forceRefresh && localBaseFolder.ContainsFolder(info.OutputFolder))
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
                    catch (Exception)
                    {
                        // TODO : log this error
                        description = new VendorDependencyDescription {Version = ""};
                    }

                    if ( info.Identity.Version.ToFullString() == description.Version)
                    {
                        return;
                    }
                }
            }
            await Download(@group.Key, info, localBaseFolder, cancelationToken);
            
            using (var descriptionStream = localBaseFolder.OpenStreamForWriting(descriptionPath))
            {
                description = new VendorDependencyDescription
                {
                    Version = info.Identity.Version.ToFullString()
                };
                var serialized = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(description, Formatting.Indented));

                await descriptionStream.WriteAsync(serialized, 0, serialized.Length, cancelationToken);
            }
        }

        private async Task Download(string sourceName, InternalPackageInformation info, ILocalBaseFolder localBaseFolder,
            CancellationToken cancelationToken)
        {
            var sourceRepository = _sources[sourceName];
            
            var remoteV3FindPackageByIdResource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>(cancelationToken);

            using (var stream = localBaseFolder.OpenStreamForWriting($@"{info.OutputFolder}\{info.Identity}.nupkg"))
            {
                await remoteV3FindPackageByIdResource.CopyNupkgToStreamAsync(info.Identity.Id, info.Identity.Version,
                    stream,
                    _sourceCacheContext,
                    NullLogger.Instance, new CancellationToken()
                );
            }
            
            using (var stream = localBaseFolder.OpenStreamForReading($@"{info.OutputFolder}\{info.Identity}.nupkg"))
            using (var compressed = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (var zipArchiveEntry in compressed.Entries)
                {
                    if (zipArchiveEntry.FullName.StartsWith("_rels") || zipArchiveEntry.Name == "[Content_Types].xml") continue;
                    
                    using (var fileStream = zipArchiveEntry.Open())
                    using (var outputStream = localBaseFolder.OpenStreamForWriting($@"{info.OutputFolder}\{zipArchiveEntry}"))
                    {
                        await fileStream.CopyToAsync(outputStream);
                    }
                };
            }
        }
    }
}