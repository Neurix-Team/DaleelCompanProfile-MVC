using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Daleel.Models
{
    public class ArticleFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "English title is required")]
        [StringLength(300, ErrorMessage = "English title cannot exceed 300 characters")]
        [Display(Name = "CmsFldEnglishTitle")]
        public string TitleEn { get; set; } = string.Empty;

        [Required(ErrorMessage = "Arabic title is required")]
        [StringLength(300, ErrorMessage = "Arabic title cannot exceed 300 characters")]
        [Display(Name = "CmsFldArabicTitle")]
        public string TitleAr { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "Slug cannot exceed 250 characters")]
        [Display(Name = "CmsFldSlugOptional")]
        public string? Slug { get; set; }

        [StringLength(1000, ErrorMessage = "Summary cannot exceed 1000 characters")]
        [Display(Name = "CmsFldEnglishSummary")]
        public string? SummaryEn { get; set; }

        [StringLength(1000, ErrorMessage = "Summary cannot exceed 1000 characters")]
        [Display(Name = "CmsFldArabicSummary")]
        public string? SummaryAr { get; set; }

        [Required(ErrorMessage = "English article content is required")]
        [Display(Name = "CmsFldEnglishContentHtml")]
        public string BodyHtmlEn { get; set; } = string.Empty;

        [Required(ErrorMessage = "Arabic article content is required")]
        [Display(Name = "CmsFldArabicContentHtml")]
        public string BodyHtmlAr { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "CmsFldCategory")]
        public string? Category { get; set; }

        [StringLength(500, ErrorMessage = "Tags cannot exceed 500 characters")]
        [Display(Name = "CmsFldTags")]
        public string? Tags { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "CmsFldAuthorName")]
        public string AuthorName { get; set; } = "Daleel Team";

        [Range(1, 120, ErrorMessage = "Reading time must be between 1 and 120 minutes")]
        [Display(Name = "CmsFldReadingTime")]
        public int ReadingMinutes { get; set; } = 5;

        [Display(Name = "CmsFldPublishStatus")]
        public bool IsPublished { get; set; }

        [Display(Name = "CmsFldSchedulePublication")]
        public DateTime? ScheduledPublishAt { get; set; }

        public string? ExistingCoverImagePath { get; set; }

        [Display(Name = "CmsFldCoverImage")]
        public IFormFile? CoverImage { get; set; }

        // --- SEO & Open Graph Metadata ---
        [StringLength(200, ErrorMessage = "Meta Title cannot exceed 200 characters")]
        [Display(Name = "CmsFldMetaTitleEn")]
        public string? MetaTitleEn { get; set; }

        [StringLength(200, ErrorMessage = "Meta Title cannot exceed 200 characters")]
        [Display(Name = "CmsFldMetaTitleAr")]
        public string? MetaTitleAr { get; set; }

        [StringLength(500, ErrorMessage = "Meta Description cannot exceed 500 characters")]
        [Display(Name = "CmsFldMetaDescEn")]
        public string? MetaDescriptionEn { get; set; }

        [StringLength(500, ErrorMessage = "Meta Description cannot exceed 500 characters")]
        [Display(Name = "CmsFldMetaDescAr")]
        public string? MetaDescriptionAr { get; set; }

        [StringLength(500, ErrorMessage = "Canonical URL cannot exceed 500 characters")]
        [Display(Name = "CmsFldCanonicalUrl")]
        public string? CanonicalUrl { get; set; }
    }
}
