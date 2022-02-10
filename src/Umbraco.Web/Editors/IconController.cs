using System;
using System.Collections.Generic;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
using Umbraco.Web.WebApi.Filters;

namespace Umbraco.Web.Editors
{
    [PluginController("UmbracoApi")]
    [IsBackOffice]
    [UmbracoWebApiRequireHttps]
    [UnhandedExceptionLoggerConfiguration]
    [EnableDetailedErrors]
    public class IconController : UmbracoApiController
    {
        private readonly IIconService _iconService;

        /// <summary>
        /// Initializes a new instance of the <see cref="IconController" /> class.
        /// </summary>
        /// <param name="iconService">The icon service.</param>
        public IconController(IIconService iconService) => _iconService = iconService;

        /// <summary>
        /// Gets an <see cref="IconModel" /> containing the icon name and SVG string for the specified <paramref name="iconName" /> (found at the global icons path).
        /// </summary>
        /// <param name="iconName">The icon name.</param>
        /// <returns>
        /// The <see cref="IconModel" /> for the specified <paramref name="iconName" />
        /// </returns>
        public IconModel GetIcon(string iconName) => _iconService.GetIcon(iconName);

        /// <summary>
        /// Gets a list of all SVG icons (found at the global icons path).
        /// </summary>
        /// <returns>
        /// The list of SVG icons.
        /// </returns>
        [Obsolete("This method should not be used - use GetIcons instead")]
        public IList<IconModel> GetAllIcons() => _iconService.GetAllIcons();

        /// <summary>
        /// Gets a list of all SVG icons (found at the global icons path).
        /// </summary>
        /// <returns>
        /// The list of SVG icons.
        /// </returns>
        public IReadOnlyDictionary<string, string> GetIcons() => _iconService.GetIcons();
    }
}
