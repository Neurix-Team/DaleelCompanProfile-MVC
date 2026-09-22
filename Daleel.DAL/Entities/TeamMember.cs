namespace Daleel.DAL.Entities
{
    /// <summary>
    /// Represents a team member / employee displayed on a brand's public profile.
    /// </summary>
    public class TeamMember
    {
        public int Id { get; set; }

        public int BrandId { get; set; }

        public BrandProfile? Brand { get; set; }

        /// <summary>URL-safe slug unique per brand.</summary>
        public string Slug { get; set; } = string.Empty;

        public string NameEn { get; set; } = string.Empty;

        public string NameAr { get; set; } = string.Empty;

        /// <summary>Job title / role (e.g. "Chief Technology Officer").</summary>
        public string? TitleEn { get; set; }

        public string? TitleAr { get; set; }

        public string? BioEn { get; set; }

        public string? BioAr { get; set; }

        public string? PhotoPath { get; set; }

        public string? Email { get; set; }

        public string? LinkedInUrl { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsPublished { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
