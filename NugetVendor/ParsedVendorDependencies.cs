using System.Collections.Generic;
using System.Linq;

namespace NugetVendor
{
    public class ParsedVendorDependencies
    {
        public Source[] Sources { get; set; }
        public Package[] Packages { get; set; }

        public void Validate()
        {
            var errors = Errors().ToList();
            if (errors.Any())
                throw new VendorDependenciesException(string.Join("\n", errors));
        }

        private IEnumerable<string> Errors()
        {
            var missingSources = new List<(string packageId, string sourceName)>();

            var sourceById = Sources.ToDictionary(s => s.Name);
            foreach (var package in Packages)
            {
                if (!sourceById.ContainsKey(package.SourceName))
                {
                    missingSources.Add((package.PackageId, package.SourceName));
                }
            }

            if (missingSources.Any())
            {
                foreach (var missingSource in missingSources)
                {
                    yield return
                        $"Missing source '{missingSource.sourceName}' referenced by '{missingSource.packageId}'";
                }

                yield return $"Existing sources: '{string.Join(", ", sourceById.Keys)}'";
            }
        }
    }
}