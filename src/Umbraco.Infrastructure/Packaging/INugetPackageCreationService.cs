using System.IO;
using Umbraco.Cms.Core.Packaging;

namespace Umbraco.Cms.Infrastructure.Packaging
{
    public interface INugetPackageCreationService
    {
        public Stream CreateNugetPackage(PackageDefinition packageDefinition);
    }
}
