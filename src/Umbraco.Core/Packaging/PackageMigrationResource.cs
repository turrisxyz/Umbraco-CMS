using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using Umbraco.Extensions;

namespace Umbraco.Cms.Core.Packaging
{
    public static class PackageMigrationResource
    {
        public static XDocument GetEmbeddedPackageDataManifest(Type planType)
            => GetEmbeddedPackageDataManifest(planType, out _);

        public static XDocument GetEmbeddedPackageDataManifest(Type planType, out ZipArchive zipArchive)
            => TryGetEmbeddedPackageDataManifest(planType, out XDocument packageXml, out zipArchive) ? packageXml : null;

        public static bool TryGetEmbeddedPackageDataManifest(Type planType, out XDocument packageXml, out ZipArchive zipArchive)
        {
            // Always try to get embedded XML
            packageXml = GetEmbeddedPackageXmlDoc(planType);

            // Fallback to embedded ZIP
            using Stream packageZipStream = GetEmbeddedPackageZipStream(planType);
            if (packageZipStream is not null)
            {
                zipArchive = GetPackageDataManifest(packageZipStream, out var zipPackageXml);

                // Only use XML from ZIP when it's not already available
                packageXml ??= zipPackageXml;

                // Cleanup if XML is still not available
                if (packageXml is null)
                {
                    zipArchive = null;
                }
            }
            else
            {
                zipArchive = null;
            }

            return packageXml is not null;
        }

        public static ZipArchive GetPackageDataManifest(Stream packageZipStream, out XDocument packageXml)
        {
            if (packageZipStream == null)
            {
                throw new ArgumentNullException(nameof(packageZipStream));
            }

            var zipArchive = new ZipArchive(packageZipStream, ZipArchiveMode.Read);
            ZipArchiveEntry packageXmlEntry = zipArchive.GetEntry("package.xml");
            if (packageXmlEntry == null)
            {
                packageXml = null;

                return zipArchive;
            }

            using (Stream packageXmlStream = packageXmlEntry.Open())
            using (var xmlReader = XmlReader.Create(packageXmlStream, new XmlReaderSettings
            {
                IgnoreWhitespace = true
            }))
            {
                packageXml = XDocument.Load(xmlReader);
            }

            return zipArchive;
        }

        public static string GetEmbeddedPackageDataManifestHash(Type planType)
        {
            string hash = null;

            var packageXml = GetEmbeddedPackageXmlDoc(planType);
            if (packageXml is not null)
            {
                hash += packageXml.ToString();
            }

            // SEE: HashFromStreams in the benchmarks project for how fast this is. It will run
            // on every startup for every embedded package.zip. The bigger the zip, the more time it takes.
            // But it is still very fast ~303ms for a 100MB file. This will only be an issue if there are
            // several very large package.zips.
            using Stream packageZipStream = GetEmbeddedPackageZipStream(planType);
            if (packageZipStream is not null)
            {
                hash += packageZipStream.GetStreamHash();
            }

            if (hash is null)
            {
                throw new IOException("Missing embedded resources for migration: " + planType);
            }

            return hash;
        }

        private static XDocument GetEmbeddedPackageXmlDoc(Type planType)
        {
            // Lookup the embedded resource by convention
            Stream packageXmlStream = planType.Assembly.GetManifestResourceStream($"{planType.Namespace}.package.xml");
            if (packageXmlStream == null)
            {
                return null;
            }

            XDocument packageXml;
            using (packageXmlStream)
            {
                packageXml = XDocument.Load(packageXmlStream);
            }

            return packageXml;
        }

        private static Stream GetEmbeddedPackageZipStream(Type planType)
            // Lookup the embedded resource by convention
            => planType.Assembly.GetManifestResourceStream($"{planType.Namespace}.package.zip");
    }
}
