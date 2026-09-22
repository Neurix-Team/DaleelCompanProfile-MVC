using System.Text.RegularExpressions;

namespace Daleel.BAL
{
    /// <summary>
    /// Shared URL-slug generation utilities used by all CMS entity services.
    /// </summary>
    public static class SlugHelper
    {
        /// <summary>
        /// Converts a human-readable text string into a URL-safe slug.
        /// Strips non-alphanumeric characters, lowercases, collapses whitespace/hyphens,
        /// and caps length at 150 characters.
        /// </summary>
        public static string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var value = text.ToLowerInvariant().Trim();
            value = Regex.Replace(value, @"[^a-z0-9\s-]", "");
            value = Regex.Replace(value, @"[\s-]+", "-").Trim('-');

            return value.Length > 150 ? value[..150].Trim('-') : value;
        }
    }
}
