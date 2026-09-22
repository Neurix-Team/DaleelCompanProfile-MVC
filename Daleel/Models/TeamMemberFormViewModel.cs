using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Daleel.Models
{
    public class TeamMemberFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a brand profile.")]
        [Display(Name = "CmsFldBrandCompany")]
        public int BrandId { get; set; }

        public string? Slug { get; set; }

        [Required(ErrorMessage = "English name is required.")]
        [Display(Name = "CmsFldFullNameEn")]
        [StringLength(200)]
        public string NameEn { get; set; } = string.Empty;

        [Required(ErrorMessage = "Arabic name is required.")]
        [Display(Name = "CmsFldFullNameAr")]
        [StringLength(200)]
        public string NameAr { get; set; } = string.Empty;

        [Display(Name = "CmsFldJobTitleEn")]
        [StringLength(200)]
        public string? TitleEn { get; set; }

        [Display(Name = "CmsFldJobTitleAr")]
        [StringLength(200)]
        public string? TitleAr { get; set; }

        [Display(Name = "CmsFldBioEn")]
        [StringLength(2000)]
        public string? BioEn { get; set; }

        [Display(Name = "CmsFldBioAr")]
        [StringLength(2000)]
        public string? BioAr { get; set; }

        [Display(Name = "CmsFldEmailAddress")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(254)]
        public string? Email { get; set; }

        [Display(Name = "CmsFldLinkedInUrl")]
        [Url(ErrorMessage = "Enter a valid URL.")]
        [StringLength(500)]
        public string? LinkedInUrl { get; set; }

        [Display(Name = "CmsFldProfilePhoto")]
        public IFormFile? PhotoFile { get; set; }

        public string? ExistingPhotoPath { get; set; }

        [Display(Name = "CmsFldDisplayOrder")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "CmsFldIsPublished")]
        public bool IsPublished { get; set; } = true;

        public SelectList? BrandOptions { get; set; }
    }
}
