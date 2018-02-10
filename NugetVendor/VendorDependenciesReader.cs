using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Sprache;

namespace NugetVendor
{
    public class VendorDependenciesException : Exception
    {
        public VendorDependenciesException()
        {
        }

        public VendorDependenciesException(string message) : base(message)
        {
        }

        public VendorDependenciesException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected VendorDependenciesException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

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

    public class Package
    {
        public string SourceName { get; set; }
        public string PackageId { get; set; }
        public string PackageVersion { get; set; }
        public string OutputFolder { get; set; }
    }

    public class Source
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class VendorDependenciesReader
    {
        private readonly TextReader _stream;

        public VendorDependenciesReader(TextReader stream)
        {
            _stream = stream;
        }

        private static readonly Parser<string> SourcePrefix = Parse.String("source").Text().Token();

        private static char[] sourceNameExtraChars = {'-', '_', '.'};

        private static readonly Parser<string> SourceName =
            Parse.Letter.Or(Parse.Chars(sourceNameExtraChars))
                .AtLeastOnce().Text().Token();

        private static char[] urlExtraChars = "://.-_?@&".Select(c => c).ToArray();

        private static readonly Parser<string> Url =
            Parse.LetterOrDigit.Or(Parse.Chars(urlExtraChars))
                .AtLeastOnce().Text().Token();



        

        private static readonly Parser<Source> SourceParser =
            from id in SourcePrefix
            from name in SourceName
            from url in Url
            select new Source
            {
                Name = name,
                Url = url
            };
        
        private static readonly Parser<string> PackageIdentifier = Parse.Letter.Or(Parse.Numeric)
            .Or(Parse.Chars('.', '-'))
            .AtLeastOnce().Text().Token();

        private static readonly Parser<string> Version = Parse.Number
            .Or(from dot in Parse.Char('.')
                from number in Parse.Number
                select dot + number).Text()
            .Or(from dot in Parse.Char('-')
                from number in Parse.LetterOrDigit.AtLeastOnce().Text()
                select dot + number).Text()
            .Text()
            .Many()
            .Select(parts => string.Join("", parts));

        private static readonly Parser<string> ExplicitOutputFolder =
            from @into in Parse.String("into").Token()
            from outputFolder in PackageIdentifier.Token().Text()
            select outputFolder;

        private static readonly Parser<Package> PackageParser =
            from name in SourceName
            from id in PackageIdentifier
            from version in Version
            from optional in ExplicitOutputFolder.Optional()
            select new Package
            {
                SourceName = name,
                PackageId = id,
                PackageVersion = version,
                OutputFolder = optional.GetOrDefault() ?? id
            };

        private static readonly Parser<string> Comment
            = from commentStart in Parse.Char('#').Token()
            from restOfLine in Parse.AnyChar.Many().Text()
            select commentStart + restOfLine;

        public async Task<ParsedVendorDependencies> ReadAsync()
        {
            string line;
            var sources = new List<Source>();
            var packages = new List<Package>();
            while ((line = await _stream.ReadLineAsync()) != null)
            {
                line = line.Trim();
                if (line == "") continue;

                if (Comment.TryParse(line).WasSuccessful) continue;

                var result = SourceParser.Select(s => (object) s).Or(PackageParser).Select(p => p).Parse(line);
                switch (result)
                {
                    case Package p:
                        packages.Add(p);
                        break;
                    case Source s:
                        sources.Add(s);
                        break;
                }
            }

            ;
            return new ParsedVendorDependencies
            {
                Sources = sources.ToArray(),
                Packages = packages.ToArray()
            };
        }
    }
}