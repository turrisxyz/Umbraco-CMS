using System;
using System.IO.Compression;
using System.Xml.Linq;
using Umbraco.Cms.Infrastructure.Migrations.Expressions;
using Umbraco.Cms.Infrastructure.Migrations.Expressions.Common;

namespace Umbraco.Cms.Infrastructure.Packaging
{
    public interface IImportPackageBuilder : IFluentBuilder
    {
        IExecutableBuilder FromEmbeddedResource<TPackageMigration>()
            where TPackageMigration : PackageMigrationBase;

        IExecutableBuilder FromEmbeddedResource(Type packageMigrationType);

        IExecutableBuilder FromXmlDataManifest(XDocument packageDataManifest);
    }

    public interface IImportPackageBuilder2 : IImportPackageBuilder
    {
        IExecutableBuilder FromEmbeddedResource<TPackageMigration>(ZipArchive packageZipArchive)
            where TPackageMigration : PackageMigrationBase;

        IExecutableBuilder FromEmbeddedResource(Type packageMigrationType, ZipArchive packageZipArchive);

        IExecutableBuilder FromXmlDataManifest(XDocument packageDataManifest, ZipArchive packageZipArchive);
    }

    public static class ImportPackageBuilderExtensions
    {
        public static IExecutableBuilder FromEmbeddedResource<TPackageMigration>(this IImportPackageBuilder importPackageBuilder, ZipArchive packageZipArchive)
            where TPackageMigration : PackageMigrationBase
        {
            if (importPackageBuilder is not IImportPackageBuilder2 importPackageBuilder2)
            {
                throw new NotSupportedException();
            }

            return importPackageBuilder2.FromEmbeddedResource<TPackageMigration>(packageZipArchive);
        }

        public static IExecutableBuilder FromEmbeddedResource(this IImportPackageBuilder importPackageBuilder, Type packageMigrationType, ZipArchive packageZipArchive)
        {
            if (importPackageBuilder is not IImportPackageBuilder2 importPackageBuilder2)
            {
                throw new NotSupportedException();
            }

            return importPackageBuilder2.FromEmbeddedResource(packageMigrationType, packageZipArchive);
        }

        public static IExecutableBuilder FromXmlDataManifest(this IImportPackageBuilder importPackageBuilder, XDocument packageDataManifest, ZipArchive packageZipArchive)
        {
            if (importPackageBuilder is not IImportPackageBuilder2 importPackageBuilder2)
            {
                throw new NotSupportedException();
            }

            return importPackageBuilder2.FromXmlDataManifest(packageDataManifest, packageZipArchive);
        }
    }
}
