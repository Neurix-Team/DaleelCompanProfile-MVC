namespace Daleel.DAL.Entities
{
    public enum NavigationLocation
    {
        Header = 1,
        Footer = 2
    }

    public enum FooterSection
    {
        Platform = 1,
        Company = 2,
        StayInformed = 3
    }

    /// <summary>
    /// Represents a dynamic navigation link displayed in the public header or footer.
    /// </summary>
    public class NavigationLink
    {
        public int Id { get; set; }

        public NavigationLocation Location { get; set; } = NavigationLocation.Header;

        /// <summary>
        /// Specific footer column (Platform, Company, StayInformed). Null for header links.
        /// </summary>
        public FooterSection? Section { get; set; }

        public string LabelEn { get; set; } = string.Empty;

        public string LabelAr { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public bool OpenInNewTab { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
