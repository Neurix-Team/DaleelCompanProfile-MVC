using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.Common;
using Daleel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Daleel.Controllers
{
    /// <summary>
    /// Site-wide colour theme: a brand palette plus one light and one dark surface shade,
    /// each picked from the presets in <see cref="SiteTheme"/>. Stored as the "site_theme"
    /// page in PageSections, which every layout reads through _ThemeStyles.
    /// </summary>
    [Route("cms/settings/theme")]
    [Authorize(Roles = "Admin")]
    public class CmsThemeController : Controller
    {
        private readonly IPageSectionService _pageSections;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public CmsThemeController(IPageSectionService pageSections, IStringLocalizer<SharedResource> localizer)
        {
            _pageSections = pageSections;
            _localizer = localizer;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var theme = SiteTheme.FromMap(await _pageSections.GetPageContentMapAsync(SiteTheme.PageKey));

            return View(new CmsThemeViewModel
            {
                Palette = theme.Palette.Key,
                Light = theme.Light.Key,
                Dark = theme.Dark.Key
            });
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CmsThemeViewModel model)
        {
            if (!SiteTheme.IsKnownPalette(model.Palette) || !SiteTheme.IsKnownLight(model.Light) || !SiteTheme.IsKnownDark(model.Dark))
            {
                TempData["Error"] = _localizer["CmsThemeInvalidChoice"].Value;
                return RedirectToAction(nameof(Index));
            }

            var result = await _pageSections.BulkUpsertAsync(SiteTheme.PageKey, new[]
            {
                Setting(SiteTheme.PaletteKey, model.Palette),
                Setting(SiteTheme.LightKey, model.Light),
                Setting(SiteTheme.DarkKey, model.Dark),
            });

            if (result.Succeeded)
            {
                TempData["Success"] = _localizer["CmsThemeSaved"].Value;
            }
            else
            {
                TempData["Error"] = _localizer["CmsThemeSaveFailed"].Value;
            }

            return RedirectToAction(nameof(Index));
        }

        private static PageSectionInput Setting(string key, string value) => new()
        {
            PageKey = SiteTheme.PageKey,
            SectionKey = key,
            ValueEn = value,
            ValueAr = value,
            DataType = "Text"
        };
    }
}
