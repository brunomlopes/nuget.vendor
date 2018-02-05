using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            _parsedVendor = reader.Read().Result;
        }
    }

    public class FetchPackagesTests
    {
        private ParsedVendorDependencies _parsedVendor;

        [Fact]
        public async Task Scratch()
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


            var v = JsonConvert.DeserializeObject<SomethingWithVersion>(content);
            v.Version.ShouldBe("1.0.0.12");

            inMemoryLocalBaseFolder.ContainsPath(@"InnovationCast.Analyzers\InnovationCast.Analyzers.1.0.0.12.nupkg").ShouldBeTrue();
        }

        class SomethingWithVersion
        {
            public string Version { get; set; }
        }

        
        private void Parse(string fileContent)
        {
            var reader = new VendorDependenciesReader(new StringReader(fileContent));
            _parsedVendor = reader.Read().Result;
        }
    }

    class InMemoryLocalBaseFolder : ILocalBaseFolder
    {
        class InMemoryFolder
        {
            public IDictionary<string, InMemoryFolder> Children = new Dictionary<string, InMemoryFolder>();
            public IDictionary<string, byte[]> Files =new Dictionary<string, byte[]>();
        }

        private InMemoryFolder Root = new InMemoryFolder();

        public bool ContainsFolder(string folderName)
        {
            return Root.Children.ContainsKey(folderName);
        }

        public async Task<string> FileContentOrEmptyAsync(string filePath, CancellationToken cancelationToken)
        {
            var now = Root;
            var path = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);
            foreach (var part in path.Split(Path.PathSeparator))
            {
                if (now.Children.ContainsKey(part)) now = now.Children[part];
                else return string.Empty;
            }

            if (now.Files.ContainsKey(fileName))
            {
                return Encoding.UTF8.GetString(now.Files[fileName]);
            }

            return string.Empty;
        }

        public bool ContainsPath(string filePath)
        {
            var now = Root;
            var path = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);
            foreach (var part in path.Split(Path.PathSeparator))
            {
                if (now.Children.ContainsKey(part)) now = now.Children[part];
                else return false;
            }

            return (now.Files.ContainsKey(fileName));
        }

        public Stream OpenStreamForWriting(string filePath)
        {
            var now = Root;
            var path = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);
            foreach (var part in path.Split(Path.PathSeparator))
            {
                if (now.Children.ContainsKey(part)) now = now.Children[part];
                else now = (now.Children[part] = new InMemoryFolder());
            }
            return new InMemoryCallbackStringStream(bytes => now.Files[fileName] = bytes);
        }
    }

    class InMemoryCallbackStringStream : Stream
    {
        private Action<byte[]> _callback;
        private MemoryStream _stream;

        public InMemoryCallbackStringStream(Action<byte[]> callback)
        {
            _callback = callback;
            _stream = new MemoryStream();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _callback?.Invoke(_stream?.GetBuffer());
                _stream?.Dispose();

                _callback = null;
                _stream = null;
            }
            
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            _callback?.Invoke(_stream.GetBuffer());
            _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }
    }
}