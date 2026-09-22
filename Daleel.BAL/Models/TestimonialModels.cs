namespace Daleel.BAL.Models
{
    public class TestimonialListItemDto
    {
        public int Id { get; set; }

        public int BrandId { get; set; }

        public string BrandNameEn { get; set; } = string.Empty;

        public string BrandNameAr { get; set; } = string.Empty;

        public string BrandSlug { get; set; } = string.Empty;

        public string CustomerNameEn { get; set; } = string.Empty;

        public string CustomerNameAr { get; set; } = string.Empty;

        public string? CompanyNameEn { get; set; }

        public string? CompanyNameAr { get; set; }

        public string? RoleTitleEn { get; set; }

        public string? RoleTitleAr { get; set; }

        public string ContentEn { get; set; } = string.Empty;

        public string ContentAr { get; set; } = string.Empty;

        public int Rating { get; set; }

        public string? PhotoPath { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsPublished { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    public class TestimonialDetailDto
    {
        public int Id { get; set; }

        public int BrandId { get; set; }

        public string BrandNameEn { get; set; } = string.Empty;

        public string BrandNameAr { get; set; } = string.Empty;

        public string BrandSlug { get; set; } = string.Empty;

        public string CustomerNameEn { get; set; } = string.Empty;

        public string CustomerNameAr { get; set; } = string.Empty;

        public string? CompanyNameEn { get; set; }

        public string? CompanyNameAr { get; set; }

        public string? RoleTitleEn { get; set; }

        public string? RoleTitleAr { get; set; }

        public string ContentEn { get; set; } = string.Empty;

        public string ContentAr { get; set; } = string.Empty;

        public int Rating { get; set; }

        public string? PhotoPath { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsPublished { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    public class TestimonialInput
    {
        public int BrandId { get; set; }

        public string CustomerNameEn { get; set; } = string.Empty;

        public string CustomerNameAr { get; set; } = string.Empty;

        public string? CompanyNameEn { get; set; }

        public string? CompanyNameAr { get; set; }

        public string? RoleTitleEn { get; set; }

        public string? RoleTitleAr { get; set; }

        public string ContentEn { get; set; } = string.Empty;

        public string ContentAr { get; set; } = string.Empty;

        public int Rating { get; set; } = 5;

        public string? PhotoPath { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsPublished { get; set; } = true;
    }

    public class TestimonialQuery
    {
        public int? BrandId { get; set; }

        public string? BrandSlug { get; set; }

        public string? Q { get; set; }

        public bool? IsPublished { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 12;
    }
}
