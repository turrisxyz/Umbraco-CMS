using System;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Models.Packaging;
using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Core.Security;
using Umbraco.Extensions;

namespace Umbraco.Cms.Infrastructure.Packaging
{
    public class NugetPackageCreationService : INugetPackageCreationService
    {
        private readonly IPackageCompilationService _compilationService;
        private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
        private readonly IUmbracoVersion _umbracoVersion;

        public NugetPackageCreationService(
            IPackageCompilationService compilationService,
            IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
            IUmbracoVersion umbracoVersion)
        {
            _compilationService = compilationService;
            _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
            _umbracoVersion = umbracoVersion;
        }

        public NugetPackage CreateNugetPackage(PackageDefinition packageDefinition)
        {
            var nameSpaceName = packageDefinition.Name.CleanStringForNamespace();
            if (string.IsNullOrEmpty(nameSpaceName))
            {
                throw new ArgumentException($"The package name is invalid: {packageDefinition.Name}");
            }

            var currentUserName = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Name;
            // This shouldn't happen, you'll be hard pressed to create a user without being logged in.
            if (currentUserName.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("Could not get current user while creating package");
            }

            var dllFileName = $"{nameSpaceName}.dll";
            // TODO: Add version number
            var nuspecFileName = $"{nameSpaceName}.nuspec";
            var metaDataFilePath =
                $"package/services/metadata/core-properties/{Guid.NewGuid().ToString().Replace("-", "")}.psmdcp";

            var nuspecFile = CreateNuspecFile(nameSpaceName, currentUserName);
            var contentTypesFile = CreateContentTypesFile();
            var metaDataFile = CreateMetaDataFile(nameSpaceName, currentUserName);
            var relationFile = CreateRelationFile(nuspecFileName, metaDataFilePath);

            var zipStream = new MemoryStream();
            using Stream dllStream = _compilationService.CreateCompiledPackage(packageDefinition);
            using (var nuGetPackage = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                // Package DLL
                ZipArchiveEntry packageDllEntry = nuGetPackage.CreateEntry($"lib/netstandard2.0/{dllFileName}");
                using (Stream dllEntryStream = packageDllEntry.Open())
                {
                    dllStream.Seek(0, SeekOrigin.Begin);
                    dllStream.CopyTo(dllEntryStream);
                }

                // Write nuspec file
                ZipArchiveEntry nuspecEntry = nuGetPackage.CreateEntry(nuspecFileName);
                using (Stream nuspecStream = nuspecEntry.Open())
                {
                    nuspecFile.Save(nuspecStream, SaveOptions.OmitDuplicateNamespaces);
                }

                // Write [Content_Types].xml
                ZipArchiveEntry contentTypesEntry = nuGetPackage.CreateEntry("[Content_Types].xml");
                using (Stream contentTypeStream = contentTypesEntry.Open())
                {
                    contentTypesFile.Save(contentTypeStream, SaveOptions.OmitDuplicateNamespaces);
                }

                ZipArchiveEntry metaDataEntry = nuGetPackage.CreateEntry(metaDataFilePath);
                using (Stream metaDataStream = metaDataEntry.Open())
                {
                    metaDataFile.Save(metaDataStream);
                }

                ZipArchiveEntry relEntry = nuGetPackage.CreateEntry("_rels/.rels");
                using (Stream relStream = relEntry.Open())
                {
                    relationFile.Save(relStream);
                }
            }

            // TODO: Add version number
            var packageFileName = $"{nameSpaceName}.1.0.0-rc003.nupkg";
            zipStream.Seek(0, SeekOrigin.Begin);
            return new NugetPackage(packageFileName, zipStream);
        }

        private XDocument CreateNuspecFile(string nameSpaceName, string currentUserName)
        {
            var nameSpace = XNamespace.Get("http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd");
            // This is the minimally required nuspec: https://docs.microsoft.com/en-us/nuget/reference/nuspec#required-metadata-elements
            var root = new XElement(nameSpace + "package");
            var nuspecXml = new XDocument(root);

            var metaData = new XElement(nameSpace + "metadata");
            root.Add(metaData);

            var id = new XElement(nameSpace + "id", nameSpaceName);
            metaData.Add(id);

            // TODO: Get version from package definition
            var version = new XElement(nameSpace + "version", "1.0.0-rc003");
            metaData.Add(version);

            var authors = new XElement(nameSpace + "authors", currentUserName);
            metaData.Add(authors);

            var description = new XElement(nameSpace + "description", "Auto generated package for Umbraco CMS");
            metaData.Add(description);

            var dependencies = new XElement(nameSpace + "dependencies");
            var netStandardGroup = new XElement(nameSpace + "group");
            netStandardGroup.Add(new XAttribute("targetFramework", ".NETStandard2.0"));

            var infrastructureDependency = new XElement(nameSpace + "dependency");
            infrastructureDependency.Add(new XAttribute("id", "Umbraco.Cms.Infrastructure"));
            infrastructureDependency.Add(new XAttribute("version", _umbracoVersion.SemanticVersion.ToSemanticStringWithoutBuild()));

            netStandardGroup.Add(infrastructureDependency);
            dependencies.Add(netStandardGroup);
            metaData.Add(dependencies);

            return nuspecXml;
        }

        private XDocument CreateContentTypesFile()
        {
            var nameSpace = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types");
            var root = new XElement(nameSpace + "Types");

            var contentTypesDocument = new XDocument(root);

            var rels = new XElement( nameSpace + "Default");
            rels.Add(new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml"));
            rels.Add(new XAttribute("Extension", "rels"));
            root.Add(rels);

            var properties = new XElement( nameSpace + "Default");
            properties.Add(new XAttribute("ContentType", "application/vnd.openxmlformats-package.core-properties+xml"));
            properties.Add(new XAttribute("Extension", "psmdcp"));
            root.Add(properties);


            var dll = new XElement( nameSpace + "Default");
            dll.Add(new XAttribute("ContentType", "application/octet"));
            dll.Add(new XAttribute("Extension", "dll"));
            root.Add(dll);

            var nuspec = new XElement(nameSpace + "Default");
            nuspec.Add(new XAttribute("ContentType", "application/octet"));
            nuspec.Add(new XAttribute("Extension", "nuspec"));
            root.Add(nuspec);

            return contentTypesDocument;
        }

        public XDocument CreateMetaDataFile(string namespaceName, string currentUserName)
        {
            // This is just a bruge force attempt at trying to recreate the psmdcp file
            XNamespace defaultNamespace = "http://schemas.openxmlformats.org/package/2006/metadata/core-properties";

            // All of these are named like they're shown in the xml
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            XNamespace dcterms = "http://purl.org/dc/terms/";
            XNamespace dc = "http://purl.org/dc/elements/1.1/";

            var root = new XElement(defaultNamespace + "coreProperties",
                new XAttribute("xmlns", "http://schemas.openxmlformats.org/package/2006/metadata/core-properties"),
                new XAttribute(XNamespace.Xmlns + "dc", "http://purl.org/dc/elements/1.1/"),
                new XAttribute(XNamespace.Xmlns + "dcterms", "http://purl.org/dc/terms/"),
                new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"));

            root.Add(new XElement(dc + "creator", currentUserName));
            root.Add(new XElement(dc + "description", "Auto generated package for Umbraco CMS"));
            root.Add(new XElement(dc + "identifier", namespaceName));
            root.Add(new XElement(defaultNamespace + "version", "1.0.0-rc003"));
            root.Add(new XElement(defaultNamespace + "keywords", ""));
            root.Add(new XElement(defaultNamespace + "lastModifiedBy", "Umbraco, Version=9.0.0, Culture=neutral"));

            return new XDocument(root);
        }

        public XDocument CreateRelationFile(string nuspecName, string metadataPath)
        {
            XNamespace nameSpace = "http://schemas.openxmlformats.org/package/2006/relationships";

            var root = new XElement(nameSpace + "Relationships");
            var nuspecEntry = new XElement(nameSpace + "Relationship");
            nuspecEntry.Add(new XAttribute("Id", "R7BA7E2CB83D382CC"));
            nuspecEntry.Add(new XAttribute("Target", $"/{nuspecName}"));
            nuspecEntry.Add(new XAttribute("Type", $"http://schemas.microsoft.com/packaging/2010/07/manifest"));
            root.Add(nuspecEntry);

            var metaEntry = new XElement(nameSpace + "Relationship");
            metaEntry.Add(new XAttribute("Id", "RA2873016BC881EDB"));
            metaEntry.Add(new XAttribute("Target", metadataPath));
            metaEntry.Add(new XAttribute("Type", "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties"));
            root.Add(metaEntry);


            return new XDocument(root);
        }
    }
}
