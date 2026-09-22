using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Daleel.Models
{
    public class BrandProfileFormViewModel
    {
        public int Id { get; set; }

        public string? Slug { get; set; }

        [Required(ErrorMessage = "English brand name is required.")]
        [Display(Name = "CmsFldBrandNameEn")]
        [StringLength(200)]
        public string NameEn { get; set; } = string.Empty;

        [Required(ErrorMessage = "Arabic brand name is required.")]
        [Display(Name = "CmsFldBrandNameAr")]
        [StringLength(200)]
        public string NameAr { get; set; } = string.Empty;

        [Display(Name = "CmsFldTaglineEn")]
        [StringLength(300)]
        public string? TaglineEn { get; set; }

        [Display(Name = "CmsFldTaglineAr")]
        [StringLength(300)]
        public string? TaglineAr { get; set; }

        [Display(Name = "CmsFldAboutEn")]
        public string? DescriptionEn { get; set; }

        [Display(Name = "CmsFldAboutAr")]
        public string? DescriptionAr { get; set; }

        [Display(Name = "CmsFldLogoLight")]
        public IFormFile? LogoFile { get; set; }

        public string? ExistingLogoPath { get; set; }

        [Display(Name = "CmsFldLogoDark")]
        public IFormFile? LogoDarkFile { get; set; }

        public string? ExistingLogoDarkPath { get; set; }

        [Display(Name = "CmsFldFaviconFile")]
        public IFormFile? FaviconFile { get; set; }

        public string? ExistingFaviconPath { get; set; }

        // --- Color Palette ---
        [Display(Name = "CmsFldPrimaryColor")]
        [RegularExpression(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", ErrorMessage = "Enter a valid hex color code (e.g. #00B2EC)")]
        [StringLength(50)]
        public string? PrimaryColor { get; set; }

        [Display(Name = "CmsFldSecondaryColor")]
        [RegularExpression(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", ErrorMessage = "Enter a valid hex color code (e.g. #10B981)")]
        [StringLength(50)]
        public string? SecondaryColor { get; set; }

        [Display(Name = "CmsFldAccentColor")]
        [RegularExpression(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", ErrorMessage = "Enter a valid hex color code (e.g. #F9A01B)")]
        [StringLength(50)]
        public string? AccentColor { get; set; }

        // --- SEO & Meta Defaults ---
        [Display(Name = "CmsFldDefMetaTitleEn")]
        [StringLength(200)]
        public string? MetaTitleEn { get; set; }

        [Display(Name = "CmsFldDefMetaTitleAr")]
        [StringLength(200)]
        public string? MetaTitleAr { get; set; }

        [Display(Name = "CmsFldDefMetaDescEn")]
        [StringLength(500)]
        public string? MetaDescriptionEn { get; set; }

        [Display(Name = "CmsFldDefMetaDescAr")]
        [StringLength(500)]
        public string? MetaDescriptionAr { get; set; }

        [Display(Name = "CmsFldGoogleAnalytics")]
        [StringLength(50)]
        public string? GoogleAnalyticsId { get; set; }

        // --- Social Links ---
        [Url(ErrorMessage = "Enter a valid URL.")]
        [Display(Name = "CmsFldLinkedInPage")]
        [StringLength(500)]
        public string? LinkedInUrl { get; set; }

        [Url(ErrorMessage = "Enter a valid URL.")]
        [Display(Name = "CmsFldTwitterUrl")]
        [StringLength(500)]
        public string? TwitterUrl { get; set; }

        [Url(ErrorMessage = "Enter a valid URL.")]
        [Display(Name = "CmsFldFacebookUrl")]
        [StringLength(500)]
        public string? FacebookUrl { get; set; }

        [Url(ErrorMessage = "Enter a valid URL.")]
        [Display(Name = "CmsFldInstagramUrl")]
        [StringLength(500)]
        public string? InstagramUrl { get; set; }

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "CmsFldContactEmail")]
        [StringLength(254)]
        public string? Email { get; set; }

        [Display(Name = "CmsFldPhoneNumber")]
        [StringLength(50)]
        public string? Phone { get; set; }

        [Display(Name = "CmsFldOfficeAddress")]
        [StringLength(500)]
        public string? Address { get; set; }

        [Url(ErrorMessage = "Enter a valid URL.")]
        [Display(Name = "CmsFldWebsiteUrl")]
        [StringLength(500)]
        public string? Website { get; set; }

        [Display(Name = "CmsFldIsPublished")]
        public bool IsPublished { get; set; } = true;
    }
}
