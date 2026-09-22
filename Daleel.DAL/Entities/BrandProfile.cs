namespace Daleel.DAL.Entities
{
    /// <summary>
    /// Represents a company / brand profile managed via CMS (e.g. Daleel, Neurix).
    /// </summary>
    public class BrandProfile
    {
        public int Id { get; set; }

        /// <summary>Unique URL-safe brand identifier: e.g. "daleel", "neurix".</summary>
        public string Slug { get; set; } = string.Empty;

        public string NameEn { get; set; } = string.Empty;

        public string NameAr { get; set; } = string.Empty;

        public string? TaglineEn { get; set; }

        public string? TaglineAr { get; set; }

        public string? DescriptionEn { get; set; }

        public string? DescriptionAr { get; set; }

        public string? LogoPath { get; set; }

        public string? LogoDarkPath { get; set; }

        public string? FaviconPath { get; set; }

        public string? PrimaryColor { get; set; }

        public string? SecondaryColor { get; set; }

        public string? AccentColor { get; set; }

        public string? MetaTitleEn { get; set; }

        public string? MetaTitleAr { get; set; }

        public string? MetaDescriptionEn { get; set; }

        public string? MetaDescriptionAr { get; set; }

        public string? GoogleAnalyticsId { get; set; }

        public string? LinkedInUrl { get; set; }

        public string? TwitterUrl { get; set; }

        public string? FacebookUrl { get; set; }

        public string? InstagramUrl { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public string? Website { get; set; }

        public bool IsPublished { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
