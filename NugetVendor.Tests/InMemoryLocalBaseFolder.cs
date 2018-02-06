using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NugetVendor.Tests
{
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

        public Stream OpenStreamForReading(string filePath)
        {
            var now = Root;
            var path = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);
            foreach (var part in path.Split(Path.PathSeparator))
            {
                if (now.Children.ContainsKey(part)) now = now.Children[part];
                else now = (now.Children[part] = new InMemoryFolder());
            }
            return new MemoryStream(now.Files[fileName]);
        }
    }
}