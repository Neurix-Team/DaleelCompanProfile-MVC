namespace Daleel.DAL.Entities
{
    /// <summary>
    /// Key-value content block for editable page sections (hero, features, callouts).
    /// </summary>
    public class PageSection
    {
        public int Id { get; set; }

        /// <summary>Page identifier e.g. "home", "about", "platforms".</summary>
        public string PageKey { get; set; } = string.Empty;

        /// <summary>Section identifier within the page e.g. "hero_title", "hero_subtitle".</summary>
        public string SectionKey { get; set; } = string.Empty;

        public string ValueEn { get; set; } = string.Empty;

        public string ValueAr { get; set; } = string.Empty;

        /// <summary>Data type hint: "Text", "Html", "ImagePath".</summary>
        public string DataType { get; set; } = "Text";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
