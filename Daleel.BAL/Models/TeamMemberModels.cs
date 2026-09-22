namespace Daleel.BAL.Models
{
    public class TeamMemberListItemDto
    {
        public int Id { get; set; }

        public int BrandId { get; set; }

        public string BrandNameEn { get; set; } = string.Empty;

        public string BrandNameAr { get; set; } = string.Empty;

        public string BrandSlug { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public string NameEn { get; set; } = string.Empty;

        public string NameAr { get; set; } = string.Empty;

        public string? TitleEn { get; set; }

        public string? TitleAr { get; set; }

        public string? BioEn { get; set; }

        public string? BioAr { get; set; }

        public string? PhotoPath { get; set; }

        public string? Email { get; set; }

        public string? LinkedInUrl { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsPublished { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    public class TeamMemberDetailDto
    {
        public int Id { get; set; }

        public int BrandId { get; set; }

        public string BrandNameEn { get; set; } = string.Empty;

        public string BrandNameAr { get; set; } = string.Empty;

        public string BrandSlug { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public string NameEn { get; set; } = string.Empty;

        public string NameAr { get; set; } = string.Empty;

        public string? TitleEn { get; set; }

        public string? TitleAr { get; set; }

        public string? BioEn { get; set; }

        public string? BioAr { get; set; }

        public string? PhotoPath { get; set; }

        public string? Email { get; set; }

        public string? LinkedInUrl { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsPublished { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    public class TeamMemberInput
    {
        public int BrandId { get; set; }

        public string? Slug { get; set; }

        public string NameEn { get; set; } = string.Empty;

        public string NameAr { get; set; } = string.Empty;

        public string? TitleEn { get; set; }

        public string? TitleAr { get; set; }

        public string? BioEn { get; set; }

        public string? BioAr { get; set; }

        public string? PhotoPath { get; set; }

        public string? Email { get; set; }

        public string? LinkedInUrl { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsPublished { get; set; } = true;
    }

    public class TeamMemberQuery
    {
        public int? BrandId { get; set; }

        public string? BrandSlug { get; set; }

        public string? Q { get; set; }

        public bool? IsPublished { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 12;
    }
}
