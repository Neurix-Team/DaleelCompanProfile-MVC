namespace Daleel.DAL.Entities
{
    /// <summary>
    /// A CMS article / blog post supporting bilingual content, SEO meta, and scheduled publishing.
    /// </summary>
    public class Article
    {
        public int Id { get; set; }

        public string Slug { get; set; } = string.Empty;

        public string TitleEn { get; set; } = string.Empty;

        public string TitleAr { get; set; } = string.Empty;

        public string? SummaryEn { get; set; }

        public string? SummaryAr { get; set; }

        public string BodyHtmlEn { get; set; } = string.Empty;

        public string BodyHtmlAr { get; set; } = string.Empty;

        public string? CoverImagePath { get; set; }

        public string? Category { get; set; }

        /// <summary>Comma-separated list of article tags e.g. "AI, Enterprise, Cloud, Saudi Vision 2030".</summary>
        public string? Tags { get; set; }

        public string AuthorName { get; set; } = "Daleel Team";

        public int ReadingMinutes { get; set; } = 5;

        public bool IsPublished { get; set; }

        /// <summary>Timestamp when the article was first made active/published.</summary>
        public DateTime? PublishedAt { get; set; }

        /// <summary>Optional scheduled publish date and time (UTC). If in the future, article is withheld from public queries until reached.</summary>
        public DateTime? ScheduledPublishAt { get; set; }

        // --- SEO & Social Sharing Metadata ---
        public string? MetaTitleEn { get; set; }

        public string? MetaTitleAr { get; set; }

        public string? MetaDescriptionEn { get; set; }

        public string? MetaDescriptionAr { get; set; }

        public string? CanonicalUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

