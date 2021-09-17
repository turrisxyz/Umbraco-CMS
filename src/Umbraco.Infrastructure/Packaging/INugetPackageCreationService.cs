using Umbraco.Cms.Core.Models.Packaging;
using Umbraco.Cms.Core.Packaging;

namespace Umbraco.Cms.Infrastructure.Packaging
{
    public interface INugetPackageCreationService
    {
        public NugetPackage CreateNugetPackage(PackageDefinition packageDefinition);
    }
}
