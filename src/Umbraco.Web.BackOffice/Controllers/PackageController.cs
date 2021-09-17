using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.ActionsResults;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.Authorization;
using Constants = Umbraco.Cms.Core.Constants;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Install;
using Umbraco.Cms.Infrastructure.Packaging;
using Umbraco.Extensions;

namespace Umbraco.Cms.Web.BackOffice.Controllers
{
    /// <summary>
    /// A controller used for managing packages in the back office
    /// </summary>
    [PluginController(Constants.Web.Mvc.BackOfficeApiArea)]
    [Authorize(Policy = AuthorizationPolicies.SectionAccessPackages)]
    public class PackageController : UmbracoAuthorizedJsonController
    {
        private readonly IPackagingService _packagingService;
        private readonly IBackOfficeSecurityAccessor _backofficeSecurityAccessor;
        private readonly PackageMigrationRunner _packageMigrationRunner;
        private readonly ILogger<PackageController> _logger;
        private readonly IShortStringHelper _shortStringHelper;
        private readonly INugetPackageCreationService _nugetPackageCreationService;

        public PackageController(
            IPackagingService packagingService,
            IBackOfficeSecurityAccessor backofficeSecurityAccessor,
            PackageMigrationRunner packageMigrationRunner,
            ILogger<PackageController> logger,
            IShortStringHelper shortStringHelper,
            INugetPackageCreationService nugetPackageCreationService)
        {
            _packagingService = packagingService ?? throw new ArgumentNullException(nameof(packagingService));
            _backofficeSecurityAccessor = backofficeSecurityAccessor ?? throw new ArgumentNullException(nameof(backofficeSecurityAccessor));
            _packageMigrationRunner = packageMigrationRunner;
            _logger = logger;
            _shortStringHelper = shortStringHelper;
            _nugetPackageCreationService = nugetPackageCreationService;
        }

        public IEnumerable<PackageDefinition> GetCreatedPackages()
        {
            return _packagingService.GetAllCreatedPackages();
        }

        public ActionResult<PackageDefinition> GetCreatedPackageById(int id)
        {
            var package = _packagingService.GetCreatedPackageById(id);
            if (package == null)
                return NotFound();

            return package;
        }

        public PackageDefinition GetEmpty() => new PackageDefinition();

        /// <summary>
        /// Creates or updates a package
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ActionResult<PackageDefinition> PostSavePackage(PackageDefinition model)
        {
            if (ModelState.IsValid == false)
                return ValidationProblem(ModelState);

            //save it
            if (!_packagingService.SaveCreatedPackage(model))
            {
                return ValidationProblem(
                        model.Id == default
                            ? $"A package with the name {model.Name} already exists"
                            : $"The package with id {model.Id} was not found");
            }

            _packagingService.ExportCreatedPackage(model);

            //the packagePath will be on the model
            return model;
        }

        /// <summary>
        /// Deletes a created package
        /// </summary>
        /// <param name="packageId"></param>
        /// <returns></returns>
        [HttpPost]
        [HttpDelete]
        public IActionResult DeleteCreatedPackage(int packageId)
        {
            _packagingService.DeleteCreatedPackage(packageId, _backofficeSecurityAccessor.BackOfficeSecurity.GetUserId().ResultOr(0));

            return Ok();
        }

        [HttpPost]
        public ActionResult<IEnumerable<InstalledPackage>> RunMigrations([FromQuery]string packageName)
        {
            try
            {
                _packageMigrationRunner.RunPackageMigrationsIfPending(packageName);
                return _packagingService.GetAllInstalledPackages().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Package migration failed on package {Package}", packageName);

                return ValidationErrorResult.CreateNotificationValidationErrorResult(
                    $"Package migration failed on package {packageName} with error: {ex.Message}. Check log for full details.");
            }
        }

        [HttpGet]
        public IActionResult DownloadCreatedPackage(int id)
        {
            var package = _packagingService.GetCreatedPackageById(id);
            if (package == null)
                return NotFound();

            if (!System.IO.File.Exists(package.PackagePath))
                return ValidationProblem("No file found for path " + package.PackagePath);

            var fileName = Path.GetFileName(package.PackagePath);

            var encoding = Encoding.UTF8;

            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = WebUtility.UrlEncode(fileName),
                Inline = false  // false = prompt the user for downloading;  true = browser to try to show the file inline
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            // Set custom header so umbRequestHelper.downloadFile can save the correct filename
            Response.Headers.Add("x-filename", WebUtility.UrlEncode(fileName));
            return new FileStreamResult(System.IO.File.OpenRead(package.PackagePath), new MediaTypeHeaderValue("application/octet-stream")
            {
                Charset = encoding.WebName,
            });

        }

        [HttpGet]
        public IActionResult DownloadNuGetPackage(int id)
        {
            PackageDefinition package = _packagingService.GetCreatedPackageById(id);
            if (package == null)
            {
                return NotFound();
            }

            if (!System.IO.File.Exists(package.PackagePath))
            {
                return ValidationProblem("No file found for path " + package.PackagePath);
            }

            // TODO: Use Version number from package definition
            // TODO: This must be the same as the ID in the NuSpec
            var fileName = $"{package.Name.ToSafeAlias(_shortStringHelper)}.1.0.0-rc003.nupkg";

            // Set custom header so umbRequestHelper.downloadFile can save the correct filename
            Response.Headers.Add("x-filename", WebUtility.UrlEncode(fileName));

            Stream stream = _nugetPackageCreationService.CreateNugetPackage(package);
            stream.Position = 0;
            return new FileStreamResult(stream, new MediaTypeHeaderValue("application/octet-stream"));

        }

        public ActionResult<InstalledPackage> GetInstalledPackageByName([FromQuery] string packageName)
        {
            InstalledPackage pack = _packagingService.GetInstalledPackageByName(packageName);
            if (pack == null)
            {
                return NotFound();
            }

            return pack;
        }

        /// <summary>
        /// Returns all installed packages - only shows their latest versions
        /// </summary>
        /// <returns></returns>
        public IEnumerable<InstalledPackage> GetInstalled()
            => _packagingService.GetAllInstalledPackages().ToList();
    }
}
