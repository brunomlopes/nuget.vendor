using System.Linq;
using Colorful;
using NugetVendor.Resolver;
using NugetVendor.Resolver.Events;
using NugetVendor.VendorDependenciesReader;

namespace NugetVendor.Output
{
    internal class SimpleRender : IRenderEvent
    {
        private readonly int _packageIdMaxLength;

        public SimpleRender(ParsedVendorDependencies deps)
        {
            _packageIdMaxLength = deps.Packages
                                      .Select(p => GetPackagePrefix(p).Length)
                                      .Max() + 1;
            Console.WriteLine();
        }

        public void Render(EngineEvent evt)
        {
            void WritePrefix(Package package)
            {
                var prefix = GetPackagePrefix(package);
                Console.Write(prefix);
                Console.Write(string.Join("",
                    Enumerable.Repeat(" ", _packageIdMaxLength - prefix.Length)));
            }

            switch (evt)
            {
                case AlreadyUpToDate already:
                    WritePrefix(already.Package);
                    Console.WriteLine("already up-to-date");
                    break;
                case Done done:
                    WritePrefix(done.Package);
                    Console.WriteLine("done");
                    break;
                case AllDone allDone:
                    Console.WriteLine("\nOk");
                    break;
            }
        }

        private static string GetPackagePrefix(Package package)
        {
            return $"{package.PackageId} ({package.PackageVersion}) ";
        }
    }
}