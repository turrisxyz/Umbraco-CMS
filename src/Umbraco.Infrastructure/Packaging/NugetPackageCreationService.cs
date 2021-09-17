using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Umbraco.Cms.Infrastructure.Packaging
{
    public class NugetPackageCreationService : INugetPackageCreationService
    {
        private readonly IPackageCompilationService _compilationService;
        private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

        public NugetPackageCreationService(
            IPackageCompilationService compilationService,
            IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
        {
            _compilationService = compilationService;
            _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        }

        public Stream CreateNugetPackage(PackageDefinition packageDefinition)
        {
            var packageFileName = $"{packageDefinition.Name.CleanStringForNamespace()}.dll";
            // TODO: Add version number
            var nuspecFileName = $"{packageDefinition.Name.CleanStringForNamespace()}.nuspec";

            var nuspecFile = CreateNuspecFile(packageDefinition);
            var contentTypesFile = CreateContentTypesFile();

            var zipStream = new MemoryStream();
            using Stream dllStream = _compilationService.CreateCompiledPackage(packageDefinition);
            using (var nuGetPackage = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                // Package DLL
                ZipArchiveEntry packageDllEntry = nuGetPackage.CreateEntry($"lib/netstandard2.0/{packageFileName}");
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
            }

            return zipStream;
        }

        private XDocument CreateNuspecFile(PackageDefinition packageDefinition)
        {
            var nameSpace = XNamespace.Get("http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd");
            // This is the minimally required nuspec: https://docs.microsoft.com/en-us/nuget/reference/nuspec#required-metadata-elements
            var root = new XElement(nameSpace + "package");
            var nuspecXml = new XDocument(root);

            var metaData = new XElement(nameSpace + "metadata");
            root.Add(metaData);

            // TODO: Ensure ID doesn't come out as empty string.
            var id = new XElement(nameSpace + "id", packageDefinition.Name.CleanStringForNamespace());
            metaData.Add(id);

            // TODO: Get version from package definition
            var version = new XElement(nameSpace + "version", "1.0.0-rc003");
            metaData.Add(version);

            var currentUserName = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Name;
            // This shouldn't happen, you'll be hard pressed to create a user without being logged in.
            if (currentUserName.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("Could not get current user while creating package");
            }

            var authors = new XElement(nameSpace + "authors", currentUserName);
            metaData.Add(authors);

            var description = new XElement(nameSpace + "description", "Auto generated package for Umbraco CMS");
            metaData.Add(description);

            var dependencies = new XElement(nameSpace + "dependencies");
            var infrastructureDependency = new XElement(nameSpace + "dependency");
            infrastructureDependency.Add(new XAttribute("id", "Umbraco.Cms.Infrastructure"));
            infrastructureDependency.Add(new XAttribute("version", "9.0.0-rc003"));
            dependencies.Add(infrastructureDependency);
            metaData.Add(dependencies);

            return nuspecXml;
        }

        private XDocument CreateContentTypesFile()
        {
            var nameSpace = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types");
            var root = new XElement(nameSpace + "Types");

            var contentTypesDocument = new XDocument(root);


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
    }
}
