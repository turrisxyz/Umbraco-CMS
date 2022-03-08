using System;
using System.IO.Compression;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Expressions;
using Umbraco.Cms.Infrastructure.Migrations.Expressions.Common;

namespace Umbraco.Cms.Infrastructure.Packaging
{
    internal class ImportPackageBuilder : ExpressionBuilderBase<ImportPackageBuilderExpression>, IImportPackageBuilder2, IExecutableBuilder
    {
        public ImportPackageBuilder(
            IPackagingService packagingService,
            IMediaService mediaService,
            MediaFileManager mediaFileManager,
            MediaUrlGeneratorCollection mediaUrlGenerators,
            IShortStringHelper shortStringHelper,
            IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
            IMigrationContext context,
            IOptions<PackageMigrationSettings> options)
            : base(new ImportPackageBuilderExpression(
                packagingService,
                mediaService,
                mediaFileManager,
                mediaUrlGenerators,
                shortStringHelper,
                contentTypeBaseServiceProvider,
                context,
                options))
        {
        }

        public void Do() => Expression.Execute();

        public IExecutableBuilder FromEmbeddedResource<TPackageMigration>()
            where TPackageMigration : PackageMigrationBase
            => FromEmbeddedResource(typeof(TPackageMigration));

        public IExecutableBuilder FromEmbeddedResource<TPackageMigration>(ZipArchive packageZipArchive)
            where TPackageMigration : PackageMigrationBase
            => FromEmbeddedResource(typeof(TPackageMigration), packageZipArchive);

        public IExecutableBuilder FromEmbeddedResource(Type packageMigrationType)
        {
            Expression.EmbeddedResourceMigrationType = packageMigrationType;
            return this;
        }

        public IExecutableBuilder FromEmbeddedResource(Type packageMigrationType, ZipArchive packageZipArchive)
        {
            Expression.EmbeddedResourceMigrationType = packageMigrationType;
            Expression.PackageZipArchive = packageZipArchive;
            return this;
        }

        public IExecutableBuilder FromXmlDataManifest(XDocument packageDataManifest)
        {
            Expression.PackageDataManifest = packageDataManifest;
            return this;
        }

        public IExecutableBuilder FromXmlDataManifest(XDocument packageDataManifest, ZipArchive packageZipArchive)
        {
            Expression.PackageDataManifest = packageDataManifest;
            Expression.PackageZipArchive = packageZipArchive;
            return this;
        }
    }
}
