using System.Text.RegularExpressions;
using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.Common;
using Daleel.Controllers;
using Daleel.Models;
using Daleel.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Daleel.Tests.Web.Shared
{
    public class SiteThemeTests
    {
        [Fact]
        public void FromMap_WithNoSettings_UsesDefaultsAndEmitsNoCss()
        {
            var theme = SiteTheme.FromMap(null);

            theme.Palette.Key.Should().Be(SiteTheme.DefaultPaletteKey);
            theme.Light.Key.Should().Be(SiteTheme.DefaultLightKey);
            theme.Dark.Key.Should().Be(SiteTheme.DefaultDarkKey);
            SiteTheme.BuildCss(theme).Should().BeEmpty();
        }

        [Fact]
        public void FromMap_WithUnknownValues_FallsBackToDefaultsSoNoRawCssIsEmitted()
        {
            var theme = SiteTheme.FromMap(new Dictionary<string, string>
            {
                [SiteTheme.PaletteKey] = "red;}body{display:none",
                [SiteTheme.LightKey] = "</style><script>",
                [SiteTheme.DarkKey] = "nope"
            });

            SiteTheme.BuildCss(theme).Should().BeEmpty();
        }

        [Fact]
        public void BuildCss_OnlyEmitsTheGroupsThatChanged()
        {
            var css = SiteTheme.BuildCss(SiteTheme.FromMap(new Dictionary<string, string>
            {
                [SiteTheme.PaletteKey] = "emerald"
            }));

            css.Should().StartWith(":root{").And.EndWith("}");
            css.Should().Contain("--th-sky:16 185 129;");
            css.Should().Contain("--th-navy:6 78 59;");
            css.Should().NotContain("--th-bg").And.NotContain("--th-d-bg");
        }

        [Fact]
        public void BuildCss_WithDarkShade_EmitsEveryDarkSurface()
        {
            var css = SiteTheme.BuildCss(SiteTheme.FromMap(new Dictionary<string, string>
            {
                [SiteTheme.DarkKey] = "charcoal",
                [SiteTheme.LightKey] = "ivory"
            }));

            foreach (var token in new[] { "th-d-bg", "th-d-card", "th-d-raised", "th-d-high", "th-d-deep", "th-d-sunk", "th-bg", "th-card" })
            {
                css.Should().Contain($"--{token}:");
            }
            css.Should().NotContain("--th-sky");
        }

        [Fact]
        public void Presets_HaveUniqueKeysAndValidHexValues()
        {
            SiteTheme.Palettes.Select(p => p.Key).Should().OnlyHaveUniqueItems();
            SiteTheme.LightShades.Select(s => s.Key).Should().OnlyHaveUniqueItems();
            SiteTheme.DarkShades.Select(s => s.Key).Should().OnlyHaveUniqueItems();

            var hexes = SiteTheme.Palettes.SelectMany(p => new[] { p.Sky, p.SkyStrong, p.SkyDeep, p.Navy })
                .Concat(SiteTheme.LightShades.SelectMany(s => new[] { s.Background, s.Card }))
                .Concat(SiteTheme.DarkShades.SelectMany(s => new[] { s.Background, s.Card, s.Raised, s.High, s.Deep, s.Sunk }));

            hexes.Should().OnlyContain(h => Regex.IsMatch(h, "^#[0-9A-Fa-f]{6}$"));
        }

        [Fact]
        public void DefaultPresets_MatchTheTailwindRootVariables()
        {
            var css = File.ReadAllText(Path.Combine(FindSolutionRoot(), "Daleel", "Styles", "tailwind.css"));
            string Root(string name) => Regex.Match(css, $@"--{name}:\s*([0-9 ]+);").Groups[1].Value.Trim();

            var palette = SiteTheme.FindPalette(SiteTheme.DefaultPaletteKey);
            var light = SiteTheme.FindLight(SiteTheme.DefaultLightKey);
            var dark = SiteTheme.FindDark(SiteTheme.DefaultDarkKey);

            Root("th-sky").Should().Be(SiteTheme.ToChannels(palette.Sky));
            Root("th-sky-strong").Should().Be(SiteTheme.ToChannels(palette.SkyStrong));
            Root("th-sky-deep").Should().Be(SiteTheme.ToChannels(palette.SkyDeep));
            Root("th-navy").Should().Be(SiteTheme.ToChannels(palette.Navy));
            Root("th-bg").Should().Be(SiteTheme.ToChannels(light.Background));
            Root("th-card").Should().Be(SiteTheme.ToChannels(light.Card));
            Root("th-d-bg").Should().Be(SiteTheme.ToChannels(dark.Background));
            Root("th-d-card").Should().Be(SiteTheme.ToChannels(dark.Card));
            Root("th-d-raised").Should().Be(SiteTheme.ToChannels(dark.Raised));
            Root("th-d-high").Should().Be(SiteTheme.ToChannels(dark.High));
            Root("th-d-deep").Should().Be(SiteTheme.ToChannels(dark.Deep));
            Root("th-d-sunk").Should().Be(SiteTheme.ToChannels(dark.Sunk));
        }

        [Fact]
        public async Task CmsThemeController_Post_RejectsUnknownPresetsWithoutSaving()
        {
            var sections = new Mock<IPageSectionService>();
            var controller = new CmsThemeController(sections.Object, Localizer()).WithTestContext();

            var result = await controller.Index(new CmsThemeViewModel { Palette = "hacked", Light = "snow", Dark = "navy" });

            result.Should().BeOfType<RedirectToActionResult>();
            controller.TempData["Error"].Should().NotBeNull();
            sections.Verify(s => s.BulkUpsertAsync(It.IsAny<string>(), It.IsAny<IEnumerable<PageSectionInput>>()), Times.Never);
        }

        [Fact]
        public async Task CmsThemeController_Post_SavesThePresetKeysToTheThemePage()
        {
            var sections = new Mock<IPageSectionService>();
            IEnumerable<PageSectionInput>? saved = null;
            sections.Setup(s => s.BulkUpsertAsync(SiteTheme.PageKey, It.IsAny<IEnumerable<PageSectionInput>>()))
                .Callback<string, IEnumerable<PageSectionInput>>((_, inputs) => saved = inputs.ToList())
                .ReturnsAsync(ServiceResult.Success());
            var controller = new CmsThemeController(sections.Object, Localizer()).WithTestContext();

            await controller.Index(new CmsThemeViewModel { Palette = "violet", Light = "mist", Dark = "slate" });

            saved.Should().NotBeNull();
            saved!.ToDictionary(s => s.SectionKey, s => s.ValueEn).Should().BeEquivalentTo(new Dictionary<string, string?>
            {
                [SiteTheme.PaletteKey] = "violet",
                [SiteTheme.LightKey] = "mist",
                [SiteTheme.DarkKey] = "slate"
            });
            controller.TempData["Success"].Should().NotBeNull();
        }

        [Fact]
        public async Task CmsPageController_HidesAndGuardsTheThemePage()
        {
            var sections = new Mock<IPageSectionService>();
            sections.Setup(s => s.GetDistinctPagesAsync()).ReturnsAsync(new List<string> { "home", SiteTheme.PageKey });
            var controller = new CmsPageController(sections.Object).WithTestContext();

            var index = (ViewResult)await controller.Index();
            ((IEnumerable<string>)index.Model!).Should().Equal("home");

            var edit = await controller.Edit(SiteTheme.PageKey, new PageSectionsEditViewModel());
            edit.Should().BeOfType<RedirectToActionResult>().Which.ControllerName.Should().Be("CmsTheme");
            sections.Verify(s => s.BulkUpsertAsync(It.IsAny<string>(), It.IsAny<IEnumerable<PageSectionInput>>()), Times.Never);
        }

        private static IStringLocalizer<SharedResource> Localizer()
        {
            var localizer = new Mock<IStringLocalizer<SharedResource>>();
            localizer.Setup(l => l[It.IsAny<string>()]).Returns<string>(key => new LocalizedString(key, key));
            return localizer.Object;
        }

        private static string FindSolutionRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Daleel.slnx")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the Daleel solution root.");
        }
    }
}
