using System.IO;
using System.Text;
using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.ModelsBuilder;
using Umbraco.Extensions;

namespace Umbraco.Cms.Infrastructure.Packaging
{
    public class PackageCompilationService : IPackageCompilationService
    {
        private readonly IShortStringHelper _shortStringHelper;

        public PackageCompilationService(IShortStringHelper shortStringHelper)
        {
            _shortStringHelper = shortStringHelper;
        }

        private RoslynCompiler _roslynCompiler;

        private RoslynCompiler RoslynCompiler
        {
            get
            {
                if (_roslynCompiler is not null)
                {
                    return _roslynCompiler;
                }

                _roslynCompiler = new RoslynCompiler();
                return _roslynCompiler;
            }
        }

        /// <inheritdoc />
        public Stream CreateCompiledPackage(PackageDefinition packageDefinition)
        {
            var packageName = packageDefinition.Name;
            // TODO: Get Version string from package definition.
            var migrationCode = GenerateDefaultMigrationCode(packageName, "1.0.0.0");
            FileStream packageXml = File.OpenRead(packageDefinition.PackagePath);

            return RoslynCompiler.CompilePackage(packageName, packageXml, migrationCode);
        }

        private string GenerateDefaultMigrationCode(string packageName, string versionString)
        {
            var builder = new StringBuilder();

            // Using statement
            builder.AppendLine("using Umbraco.Cms.Infrastructure.Packaging;");

            // Add Assembly version
            builder.Append("[assembly:System.Reflection.AssemblyVersion(\"");
            builder.Append(versionString);
            builder.AppendLine("\")]");

            // Namespace
            // We don't want spaces in the package name since it will mess with the namespace, for now use ToSafeAlias
            // TODO: Should we handle this differently? How do we currently handle it when installing packages?
            builder.Append("namespace ");
            builder.AppendLine(packageName.ToSafeAlias(_shortStringHelper));
            builder.AppendLine("{");

            // Class definition
            builder.Append("public class DefaultMigration : ");
            builder.AppendLine("AutomaticPackageMigrationPlan {");

            // Constructor
            builder.Append("public DefaultMigration() : base(\"");
            builder.AppendLine($"{packageName}\")");
            builder.AppendLine("{}");

            // Close the brackets.
            builder.AppendLine("}}");

            return builder.ToString();
        }

    }
}
