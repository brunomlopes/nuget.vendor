using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NugetVendor.Resolver
{
    public class LocalBaseFolder : ILocalBaseFolder
    {
        private readonly DirectoryInfo _baseDir;
        public LocalBaseFolder(string basePath)
        {
            _baseDir = new DirectoryInfo(basePath);
            if (!_baseDir.Exists)
            {
                throw new InvalidOperationException($"Missing path  '{_baseDir.FullName}'");
            }
                
        }
        public bool ContainsFolder(string folderName)
        {
            return _baseDir.EnumerateDirectories(folderName, SearchOption.TopDirectoryOnly).FirstOrDefault(d => d.Name == folderName) != null;
        }

        public async Task<string> FileContentOrEmptyAsync(string filePath, CancellationToken cancelationToken)
        {
            var fullPath = Path.Combine(_baseDir.FullName, filePath);
            if (!File.Exists(fullPath)) return String.Empty;
            return await File.ReadAllTextAsync(fullPath, cancelationToken);
        }

        public Stream OpenStreamForWriting(string filePath)
        {
            var fullPath = Path.Combine(_baseDir.FullName, filePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            if(File.Exists(fullPath)) File.Delete(fullPath);
            return File.OpenWrite(fullPath);
        }

        public Stream OpenStreamForReading(string filePath)
        {
            var fullPath = Path.Combine(_baseDir.FullName, filePath);
            return File.OpenRead(fullPath);
        }
    }
}