using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NugetVendor.VendorDependenciesReader;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugetVendor.Resolver
{
    public abstract class EngineEvent
    {
    }

    public interface IPackageEngineEvent
    {
        Package Package { get; set; }
    }

    public class AlreadyUpToDate : EngineEvent, IPackageEngineEvent
    {
        public AlreadyUpToDate(Package package)
        {
            Package = package;
        }

        public Package Package { get; set; }
    }

    public class Done : EngineEvent, IPackageEngineEvent
    {
        public Done(Package package)
        {
            Package = package;
        }

        public Package Package { get; set; }
    }

    public class Resolving : EngineEvent, IPackageEngineEvent
    {
        public Resolving(Package package, Source source)
        {
            Package = package;
            Source = source;
        }

        public Package Package { get; set; }
        public Source Source { get; set; }
    }

    public class Downloading : EngineEvent, IPackageEngineEvent
    {
        public Downloading(Package package, Source source)
        {
            Package = package;
            Source = source;
        }

        public Package Package { get; set; }
        public Source Source { get; set; }
    }

    public class Downloaded : EngineEvent, IPackageEngineEvent
    {
        public Downloaded(Package package, Source source)
        {
            Package = package;
            Source = source;
        }

        public Package Package { get; set; }
        public Source Source { get; set; }
    }

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


    public delegate void EngineEventListener(EngineEvent evt);

    public class ResolveEngine
    {
        private ParsedVendorDependencies _vendorDependencies;
        private readonly List<Lazy<INuGetResourceProvider>> _providers;
        private Dictionary<string, SourceRepository> _sourceRepositories;
        private Dictionary<string, Source> _sources;
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

        private EngineEventListener listeners = evt => { };

        public ResolveEngine Listen(EngineEventListener listener)
        {
            listeners += listener;
            return this;
        }

        public ResolveEngine ForceRefresh()
        {
            _forceRefresh = true;
            return this;
        }

        class InternalPackageInformation
        {
            public Package Package { get; set; }
            public Source Source { get; set; }
            public PackageIdentity Identity { get; set; }
        }

        public ResolveEngine Initialize(ParsedVendorDependencies vendorDependencies)
        {
            _vendorDependencies = vendorDependencies;
            _sourceRepositories = _vendorDependencies.Sources
                .ToDictionary(s => s.Name, s => new SourceRepository(new PackageSource(s.Url), _providers));
            _sources = _vendorDependencies.Sources.ToDictionary(s => s.Name);
            _packageIdentitiesBySourceName = _vendorDependencies.Packages
                .GroupBy(p => p.SourceName)
                .ToDictionary(g => g.Key,
                    g => g.Select(p => new InternalPackageInformation
                    {
                        Package = p,
                        Source = _sources[p.SourceName],
                        Identity = new PackageIdentity(p.PackageId, NuGetVersion.Parse(p.PackageVersion))
                    }));
            return this;
        }

        public class VendorDependencyDescription
        {
            public string Version { get; set; }
        }

        public async Task RunAsync(ILocalBaseFolder localBaseFolder)
        {
            var cancelationToken = new CancellationToken();
            var runningTasks = _packageIdentitiesBySourceName
                .AsParallel()
                .Select(group => DownloadVendorsFromSourceName(localBaseFolder, group, cancelationToken));

            await Task.WhenAll(runningTasks);
        }

        private async Task DownloadVendorsFromSourceName(ILocalBaseFolder localBaseFolder,
            KeyValuePair<string, IEnumerable<InternalPackageInformation>> group,
            CancellationToken cancelationToken)
        {
            foreach (var package in group.Value)
            {
                await DownloadPackage(localBaseFolder, group.Key, cancelationToken, package);
            }
        }

        private async Task DownloadPackage(ILocalBaseFolder localBaseFolder,
            string sourceName,
            CancellationToken cancelationToken, InternalPackageInformation info)
        {
            var descriptionPath = $@"{info.Package.OutputFolder}\vendor.dependency.description.json";
            VendorDependencyDescription description;

            if (!_forceRefresh && localBaseFolder.ContainsFolder(info.Package.OutputFolder))
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

                    if (info.Identity.Version.ToFullString() == description.Version)
                    {
                        listeners(new AlreadyUpToDate(info.Package));
                        return;
                    }
                }
            }

            await Download(sourceName, info, localBaseFolder, cancelationToken);

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

        private async Task Download(string sourceName, InternalPackageInformation info,
            ILocalBaseFolder localBaseFolder,
            CancellationToken cancelationToken)
        {
            listeners(new Resolving(info.Package, info.Source));
            var sourceRepository = _sourceRepositories[sourceName];

            var remoteV3FindPackageByIdResource =
                await sourceRepository.GetResourceAsync<FindPackageByIdResource>(cancelationToken);

            using (var stream =
                localBaseFolder.OpenStreamForWriting($@"{info.Package.OutputFolder}\{info.Identity}.nupkg"))
            {
                listeners(new Downloading(info.Package, info.Source));

                await remoteV3FindPackageByIdResource.CopyNupkgToStreamAsync(info.Identity.Id, info.Identity.Version,
                    stream,
                    _sourceCacheContext,
                    NullLogger.Instance, new CancellationToken()
                );
                listeners(new Downloaded(info.Package, info.Source));
            }

            using (var stream =
                localBaseFolder.OpenStreamForReading($@"{info.Package.OutputFolder}\{info.Identity}.nupkg"))
            using (var compressed = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                var totalCount = compressed.Entries.Count;
               
                foreach (var (zipArchiveEntry, i) in compressed.Entries.Select((e, i) => (e, i)))
                {
                    var fullName = Uri.UnescapeDataString(zipArchiveEntry.FullName);
                    var name = Uri.UnescapeDataString(zipArchiveEntry.Name);
                    listeners(new Decompressing(info.Package, info.Source, fullName, i, totalCount));

                    if (fullName.StartsWith("_rels") ||
                        name == "[Content_Types].xml") continue;

                    using (var fileStream = zipArchiveEntry.Open())
                    using (var outputStream =
                        localBaseFolder.OpenStreamForWriting($@"{info.Package.OutputFolder}\{fullName}"))
                    {
                        await fileStream.CopyToAsync(outputStream);
                    }
                }
            }
        }
    }
}