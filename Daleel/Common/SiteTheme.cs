using System.Globalization;
using System.Text;

namespace Daleel.Common
{
    /// <summary>A brand colour family: buttons, links, badges, highlights and headings.</summary>
    public sealed record ThemePalette(
        string Key, string NameEn, string NameAr,
        string Sky, string SkyStrong, string SkyDeep, string Navy);

    /// <summary>Light mode surfaces: page background and cards.</summary>
    public sealed record ThemeLightShade(
        string Key, string NameEn, string NameAr,
        string Background, string Card);

    /// <summary>Dark mode surfaces, from the deepest band to raised panels.</summary>
    public sealed record ThemeDarkShade(
        string Key, string NameEn, string NameAr,
        string Background, string Card, string Raised, string High, string Deep, string Sunk);

    /// <summary>The three choices an admin makes on the CMS Theme page.</summary>
    public sealed record SiteThemeSelection(ThemePalette Palette, ThemeLightShade Light, ThemeDarkShade Dark)
    {
        public bool IsDefaultPalette => Palette.Key == SiteTheme.DefaultPaletteKey;
        public bool IsDefaultLight => Light.Key == SiteTheme.DefaultLightKey;
        public bool IsDefaultDark => Dark.Key == SiteTheme.DefaultDarkKey;
    }

    /// <summary>
    /// Preset catalogue for the CMS Theme page and the CSS injected into every layout.
    ///
    /// The layouts colour everything through the th-* Tailwind tokens, which read "R G B"
    /// channel variables (--th-sky: 0 178 236) so opacity modifiers like bg-th-sky/10 keep
    /// working. Only values from this catalogue are ever written into a &lt;style&gt; — the
    /// stored setting is just a preset key, never raw CSS.
    /// </summary>
    public static class SiteTheme
    {
        /// <summary>PageSections page that stores the selection. Hidden from the Page Sections editor.</summary>
        public const string PageKey = "site_theme";
        public const string PaletteKey = "palette";
        public const string LightKey = "light";
        public const string DarkKey = "dark";

        public const string DefaultPaletteKey = "daleel";
        public const string DefaultLightKey = "snow";
        public const string DefaultDarkKey = "navy";

        public static readonly IReadOnlyList<ThemePalette> Palettes = new[]
        {
            // Default: identical to the :root values in Styles/tailwind.css, so nothing is emitted for it.
            new ThemePalette("daleel",   "Daleel Sky",    "سماوي دليل",    "#00B2EC", "#0096C7", "#008DBB", "#1D3166"),
            new ThemePalette("royal",    "Royal Blue",    "أزرق ملكي",     "#3B82F6", "#2563EB", "#1D4ED8", "#1E3A8A"),
            new ThemePalette("ocean",    "Ocean Teal",    "أزرق محيطي",    "#06B6D4", "#0891B2", "#0E7490", "#164E63"),
            new ThemePalette("emerald",  "Emerald",       "زمردي",         "#10B981", "#059669", "#047857", "#064E3B"),
            new ThemePalette("violet",   "Violet",        "بنفسجي",        "#8B5CF6", "#7C3AED", "#6D28D9", "#2E1065"),
            new ThemePalette("indigo",   "Royal Indigo",  "نيلي ملكي",     "#6366F1", "#4F46E5", "#4338CA", "#1E1B4B"),
            new ThemePalette("sunset",   "Sunset Orange", "برتقالي غروب",  "#F97316", "#EA580C", "#C2410C", "#431407"),
            new ThemePalette("rose",     "Rose",          "وردي",          "#F43F5E", "#E11D48", "#BE123C", "#4C0519"),
            new ThemePalette("crimson",  "Crimson",       "قرمزي",         "#EF4444", "#DC2626", "#B91C1C", "#450A0A"),
            new ThemePalette("gold",     "Royal Gold",    "ذهبي",          "#D4A017", "#B8860B", "#9A7209", "#3D2E05"),
            new ThemePalette("graphite", "Graphite",      "رمادي جرافيت",  "#64748B", "#475569", "#334155", "#0F172A"),
        };

        public static readonly IReadOnlyList<ThemeLightShade> LightShades = new[]
        {
            // Default: slate-50 page and white cards, as the site shipped.
            new ThemeLightShade("snow",  "Snow (default)", "ثلجي (افتراضي)", "#F8FAFC", "#FFFFFF"),
            new ThemeLightShade("white", "Pure White",     "أبيض ناصع",      "#FFFFFF", "#FFFFFF"),
            new ThemeLightShade("mist",  "Cool Mist",      "ضبابي بارد",     "#EEF2F7", "#FBFCFE"),
            new ThemeLightShade("ivory", "Warm Ivory",     "عاجي دافئ",      "#FAF7F0", "#FFFDF8"),
            new ThemeLightShade("pearl", "Pearl Gray",     "رمادي لؤلؤي",    "#F2F2F2", "#FCFCFC"),
            new ThemeLightShade("mint",  "Soft Mint",      "نعناعي هادئ",    "#F2F8F5", "#FFFFFF"),
        };

        public static readonly IReadOnlyList<ThemeDarkShade> DarkShades = new[]
        {
            // Default: the Daleel navy surfaces the views were built with.
            new ThemeDarkShade("navy",     "Daleel Navy (default)", "كحلي دليل (افتراضي)", "#0A192F", "#0B1728", "#0F2542", "#112240", "#060D1A", "#0B1220"),
            new ThemeDarkShade("midnight", "Midnight",              "منتصف الليل",         "#070B14", "#0D1321", "#141C2E", "#18223A", "#04070D", "#0A0F1A"),
            new ThemeDarkShade("slate",    "Deep Slate",            "أردوازي داكن",        "#0F172A", "#111C31", "#1E293B", "#243247", "#0A1120", "#0D1526"),
            new ThemeDarkShade("charcoal", "Charcoal",              "فحمي",                "#121212", "#181818", "#222222", "#2A2A2A", "#0A0A0A", "#101010"),
            new ThemeDarkShade("black",    "Pure Black",            "أسود خالص",           "#000000", "#0A0A0A", "#141414", "#1A1A1A", "#000000", "#050505"),
            new ThemeDarkShade("plum",     "Deep Plum",             "برقوقي داكن",         "#140B1F", "#1A1028", "#241838", "#2A1C42", "#0C0614", "#110919"),
            new ThemeDarkShade("forest",   "Deep Forest",           "أخضر غابي داكن",      "#081A14", "#0B211A", "#102C23", "#13332A", "#04100C", "#071712"),
        };

        public static ThemePalette FindPalette(string? key) =>
            Palettes.FirstOrDefault(p => string.Equals(p.Key, key?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? Palettes[0];

        public static ThemeLightShade FindLight(string? key) =>
            LightShades.FirstOrDefault(s => string.Equals(s.Key, key?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? LightShades[0];

        public static ThemeDarkShade FindDark(string? key) =>
            DarkShades.FirstOrDefault(s => string.Equals(s.Key, key?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? DarkShades[0];

        public static bool IsKnownPalette(string? key) => Palettes.Any(p => p.Key == key);
        public static bool IsKnownLight(string? key) => LightShades.Any(s => s.Key == key);
        public static bool IsKnownDark(string? key) => DarkShades.Any(s => s.Key == key);

        /// <summary>Reads the selection from the site_theme page map; unknown or missing keys fall back to defaults.</summary>
        public static SiteThemeSelection FromMap(IReadOnlyDictionary<string, string>? map)
        {
            string? Value(string key) => map != null && map.TryGetValue(key, out var v) ? v : null;
            return new SiteThemeSelection(FindPalette(Value(PaletteKey)), FindLight(Value(LightKey)), FindDark(Value(DarkKey)));
        }

        /// <summary>
        /// Builds the override stylesheet. Must be emitted AFTER tailwind.css so these :root
        /// variables win by source order. Each group is left out while it is on its default.
        /// </summary>
        public static string BuildCss(SiteThemeSelection theme)
        {
            var vars = new StringBuilder();

            if (!theme.IsDefaultPalette)
            {
                var p = theme.Palette;
                Append(vars, "th-sky", p.Sky);
                Append(vars, "th-sky-strong", p.SkyStrong);
                Append(vars, "th-sky-deep", p.SkyDeep);
                Append(vars, "th-navy", p.Navy);
            }

            if (!theme.IsDefaultLight)
            {
                Append(vars, "th-bg", theme.Light.Background);
                Append(vars, "th-card", theme.Light.Card);
            }

            // Dark surfaces are also used by always-dark bands in light mode, so they live on :root too.
            if (!theme.IsDefaultDark)
            {
                var d = theme.Dark;
                Append(vars, "th-d-bg", d.Background);
                Append(vars, "th-d-card", d.Card);
                Append(vars, "th-d-raised", d.Raised);
                Append(vars, "th-d-high", d.High);
                Append(vars, "th-d-deep", d.Deep);
                Append(vars, "th-d-sunk", d.Sunk);
            }

            return vars.Length == 0 ? string.Empty : ":root{" + vars + "}";
        }

        private static void Append(StringBuilder css, string name, string hex) =>
            css.Append("--").Append(name).Append(':').Append(ToChannels(hex)).Append(';');

        /// <summary>"#00B2EC" → "0 178 236", the shape the th-* Tailwind colours expect.</summary>
        public static string ToChannels(string hex)
        {
            var h = hex.TrimStart('#');
            var r = int.Parse(h.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var g = int.Parse(h.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var b = int.Parse(h.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return string.Create(CultureInfo.InvariantCulture, $"{r} {g} {b}");
        }
    }
}
