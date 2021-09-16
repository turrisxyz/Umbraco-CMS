using System.IO;
using Umbraco.Cms.Core.Packaging;

namespace Umbraco.Cms.Infrastructure.Packaging
{
    public interface IPackageCompilationService
    {
        /// <summary>
        /// Compiles a <see cref="PackageDefinition"/> into a DLL.
        /// </summary>
        /// <param name="packageDefinition">Package definition to compile</param>
        /// <returns>A stream containing the compiled DLL</returns>
        public Stream CreateCompiledPackage(PackageDefinition packageDefinition);
    }
}
