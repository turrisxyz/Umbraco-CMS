using System.IO;

namespace Umbraco.Cms.Core.Models.Packaging
{
    public class NugetPackage
    {
        public NugetPackage()
        {

        }

        public NugetPackage(string packageName, Stream packageStream)
        {
            PackageName = packageName;
            PackageStream = packageStream;
        }

        public Stream PackageStream { get; set; }
        public string PackageName { get; set; }
    }
}
