namespace Daleel.BAL.Models
{
    public class ArticleListItem
    {
        public int Id { get; set; }

        public string Slug { get; set; } = string.Empty;

        public string TitleEn { get; set; } = string.Empty;

        public string TitleAr { get; set; } = string.Empty;

        public string? SummaryEn { get; set; }

        public string? SummaryAr { get; set; }

        public string? CoverImagePath { get; set; }

        public string? Category { get; set; }

        public string? Tags { get; set; }

        public string AuthorName { get; set; } = string.Empty;

        public int ReadingMinutes { get; set; }

        public bool IsPublished { get; set; }

        public DateTime? PublishedAt { get; set; }

        public DateTime? ScheduledPublishAt { get; set; }

        public bool IsScheduled => IsPublished && ScheduledPublishAt.HasValue && ScheduledPublishAt.Value > DateTime.UtcNow;

        public DateTime CreatedAt { get; set; }
    }

    public class ArticleDetailDto
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

        public string? Tags { get; set; }

        public string AuthorName { get; set; } = string.Empty;

        public int ReadingMinutes { get; set; }

        public bool IsPublished { get; set; }

        public DateTime? PublishedAt { get; set; }

        public DateTime? ScheduledPublishAt { get; set; }

        public bool IsScheduled => IsPublished && ScheduledPublishAt.HasValue && ScheduledPublishAt.Value > DateTime.UtcNow;

        // SEO & Social Metadata
        public string? MetaTitleEn { get; set; }

        public string? MetaTitleAr { get; set; }

        public string? MetaDescriptionEn { get; set; }

        public string? MetaDescriptionAr { get; set; }

        public string? CanonicalUrl { get; set; }

        public IReadOnlyList<string> TagsList => string.IsNullOrWhiteSpace(Tags)
            ? Array.Empty<string>()
            : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    public class ArticleInput
    {
        public string TitleEn { get; set; } = string.Empty;

        public string TitleAr { get; set; } = string.Empty;

        public string? Slug { get; set; }

        public string? SummaryEn { get; set; }

        public string? SummaryAr { get; set; }

        public string BodyHtmlEn { get; set; } = string.Empty;

        public string BodyHtmlAr { get; set; } = string.Empty;

        public string? CoverImagePath { get; set; }

        public string? Category { get; set; }

        public string? Tags { get; set; }

        public string AuthorName { get; set; } = "Daleel Team";

        public int ReadingMinutes { get; set; } = 5;

        public bool IsPublished { get; set; }

        public DateTime? ScheduledPublishAt { get; set; }

        // SEO & Social Metadata
        public string? MetaTitleEn { get; set; }

        public string? MetaTitleAr { get; set; }

        public string? MetaDescriptionEn { get; set; }

        public string? MetaDescriptionAr { get; set; }

        public string? CanonicalUrl { get; set; }
    }

    public class ArticleQuery
    {
        public string? Q { get; set; }

        public string? Category { get; set; }

        public string? Tag { get; set; }

        public bool? IsPublished { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }

    public class PageSectionDto
    {
        public int Id { get; set; }

        public string PageKey { get; set; } = string.Empty;

        public string SectionKey { get; set; } = string.Empty;

        public string ValueEn { get; set; } = string.Empty;

        public string ValueAr { get; set; } = string.Empty;

        public string DataType { get; set; } = "Text";

        public DateTime UpdatedAt { get; set; }
    }

    public class PageSectionInput
    {
        public string PageKey { get; set; } = string.Empty;

        public string SectionKey { get; set; } = string.Empty;

        // Nullable on purpose. A page section may legitimately be blank (the public views
        // fall back to their built-in copy), and non-nullable strings are treated as
        // implicitly [Required] under nullable reference types — which made clearing any
        // field fail validation instead of saving.
        public string? ValueEn { get; set; } = string.Empty;

        public string? ValueAr { get; set; } = string.Empty;

        public string DataType { get; set; } = "Text";
    }

    public class CmsDashboardStats
    {
        public int TotalArticles { get; set; }

        public int PublishedArticles { get; set; }

        public int DraftArticles { get; set; }

        public int TotalPageSections { get; set; }

        public int TotalBrands { get; set; }

        public int PublishedBrands { get; set; }

        public int TotalServices { get; set; }

        public int TotalProjects { get; set; }

        public int TotalTeamMembers { get; set; }

        public int TotalTestimonials { get; set; }

        public IReadOnlyList<ArticleListItem> RecentArticles { get; set; } = Array.Empty<ArticleListItem>();

        public IReadOnlyList<BrandProfileListItemDto> Brands { get; set; } = Array.Empty<BrandProfileListItemDto>();
    }
}
