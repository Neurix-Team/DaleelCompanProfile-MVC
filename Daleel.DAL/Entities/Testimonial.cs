namespace Daleel.DAL.Entities
{
    /// <summary>
    /// Represents a client testimonial / review for a specific brand profile.
    /// </summary>
    public class Testimonial
    {
        public int Id { get; set; }

        public int BrandId { get; set; }

        public BrandProfile? Brand { get; set; }

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

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
