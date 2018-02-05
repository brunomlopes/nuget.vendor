using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NugetVendor
{
    public interface ILocalBaseFolder
    {
        bool ContainsFolder(string folderName);
        Task<string> FileContentOrEmptyAsync(string filePath, CancellationToken cancelationToken);
        Stream OpenStreamForWriting(string path);
    }
}