using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Daleel.Models
{
    public class TestimonialFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a brand profile.")]
        [Display(Name = "CmsFldBrandCompany")]
        public int BrandId { get; set; }

        [Required(ErrorMessage = "Customer name in English is required.")]
        [Display(Name = "CmsFldCustomerNameEn")]
        [StringLength(200)]
        public string CustomerNameEn { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer name in Arabic is required.")]
        [Display(Name = "CmsFldCustomerNameAr")]
        [StringLength(200)]
        public string CustomerNameAr { get; set; } = string.Empty;

        [Display(Name = "CmsFldCompanyNameEn")]
        [StringLength(200)]
        public string? CompanyNameEn { get; set; }

        [Display(Name = "CmsFldCompanyNameAr")]
        [StringLength(200)]
        public string? CompanyNameAr { get; set; }

        [Display(Name = "CmsFldRoleTitleEn")]
        [StringLength(200)]
        public string? RoleTitleEn { get; set; }

        [Display(Name = "CmsFldRoleTitleAr")]
        [StringLength(200)]
        public string? RoleTitleAr { get; set; }

        [Required(ErrorMessage = "Testimonial content in English is required.")]
        [Display(Name = "CmsFldQuoteEn")]
        [StringLength(2000)]
        public string ContentEn { get; set; } = string.Empty;

        [Required(ErrorMessage = "Testimonial content in Arabic is required.")]
        [Display(Name = "CmsFldQuoteAr")]
        [StringLength(2000)]
        public string ContentAr { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        [Display(Name = "CmsFldRating")]
        public int Rating { get; set; } = 5;

        [Display(Name = "CmsFldCustomerPhoto")]
        public IFormFile? PhotoFile { get; set; }

        public string? ExistingPhotoPath { get; set; }

        [Display(Name = "CmsFldDisplayOrder")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "CmsFldIsPublished")]
        public bool IsPublished { get; set; } = true;

        public SelectList? BrandOptions { get; set; }
    }
}
