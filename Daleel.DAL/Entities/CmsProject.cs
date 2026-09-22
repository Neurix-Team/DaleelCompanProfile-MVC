namespace Daleel.DAL.Entities
{
    /// <summary>
    /// Represents a portfolio project / case study delivered by a brand (e.g. Daleel, Neurix).
    /// </summary>
    public class CmsProject
    {
        public int Id { get; set; }

        public int BrandId { get; set; }

        public BrandProfile? Brand { get; set; }

        /// <summary>URL-safe slug unique per brand.</summary>
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

        public int DisplayOrder { get; set; } = 0;

        public bool IsPublished { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
