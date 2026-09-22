using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Daleel.Models
{
    /// <summary>
    /// Helpers for reading CMS-editable page sections in public views.
    /// </summary>
    public static class PageContentExtensions
    {
        private static readonly IReadOnlyDictionary<string, string> Empty =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The page-section map a controller stashed in ViewData, or an empty map when the
        /// action does not supply one. Never null, so views need no guard.
        /// </summary>
        public static IReadOnlyDictionary<string, string> PageContent(this ViewDataDictionary viewData)
            => viewData["PageContent"] as IReadOnlyDictionary<string, string> ?? Empty;

        /// <summary>
        /// Value for <paramref name="sectionKey"/>, falling back to <paramref name="fallback"/>
        /// when the section is absent or blank. The fallback keeps a page rendering its original
        /// copy if the row was never seeded or an admin cleared the field, so an empty CMS value
        /// can never blank out a live page.
        /// </summary>
        public static string Section(
            this IReadOnlyDictionary<string, string> content,
            string sectionKey,
            string fallback)
            => content.TryGetValue(sectionKey, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : fallback;
    }
}
