namespace Daleel.BAL.Models
{
    public class CmsProjectListItemDto
    {
        public int Id { get; set; }

        public int BrandId { get; set; }

        public string BrandNameEn { get; set; } = string.Empty;

        public string BrandNameAr { get; set; } = string.Empty;

        public string BrandSlug { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public string NameEn { get; set; } = string.Empty;

        public string NameAr { get; set; } = string.Empty;

        public string? ShortDescEn { get; set; }

        public string? ShortDescAr { get; set; }

        public string? ClientNameEn { get; set; }

        public string? ClientNameAr { get; set; }

        public DateTime? CompletionDate { get; set; }

        public string? ProjectUrl { get; set; }

        public string? ImagePath { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsPublished { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    public class CmsProjectDetailDto
    {
        public int Id { get; set; }

        public int BrandId { get; set; }

        public string BrandNameEn { get; set; } = string.Empty;

        public string BrandNameAr { get; set; } = string.Empty;

        public string BrandSlug { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public string NameEn { get; set; } = string.Empty;

        public string NameAr { get; set; } = string.Empty;

        public string? ShortDescEn { get; set; }

        public string? ShortDescAr { get; set; }

        public string? DescriptionEn { get; set; }

        public string? DescriptionAr { get; set; }

        public string? ClientNameEn { get; set; }

        public string? ClientNameAr { get; set; }

        public DateTime? CompletionDate { get; set; }

        public string? ProjectUrl { get; set; }

        public string? ImagePath { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsPublished { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    public class CmsProjectInput
    {
        public int BrandId { get; set; }

        public string? Slug { get; set; }

        public string NameEn { get; set; } = string.Empty;

        public string NameAr { get; set; } = string.Empty;

        public string? ShortDescEn { get; set; }

        public string? ShortDescAr { get; set; }

        public string? DescriptionEn { get; set; }

        public string? DescriptionAr { get; set; }

        public string? ClientNameEn { get; set; }

        public string? ClientNameAr { get; set; }

        public DateTime? CompletionDate { get; set; }

        public string? ProjectUrl { get; set; }

        public string? ImagePath { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsPublished { get; set; } = true;
    }

    public class CmsProjectQuery
    {
        public int? BrandId { get; set; }

        public string? BrandSlug { get; set; }

        public string? Q { get; set; }

        public bool? IsPublished { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 12;
    }
}
