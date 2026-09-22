using System.ComponentModel.DataAnnotations;
using Daleel.BAL.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Daleel.Models
{
    public class CmsServiceFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a brand profile.")]
        [Display(Name = "CmsFldBrandCompany")]
        public int BrandId { get; set; }

        public string? Slug { get; set; }

        [Required(ErrorMessage = "English service name is required.")]
        [Display(Name = "CmsFldServiceNameEn")]
        [StringLength(200)]
        public string NameEn { get; set; } = string.Empty;

        [Required(ErrorMessage = "Arabic service name is required.")]
        [Display(Name = "CmsFldServiceNameAr")]
        [StringLength(200)]
        public string NameAr { get; set; } = string.Empty;

        [Display(Name = "CmsFldShortSummaryEn")]
        [StringLength(500)]
        public string? ShortDescEn { get; set; }

        [Display(Name = "CmsFldShortSummaryAr")]
        [StringLength(500)]
        public string? ShortDescAr { get; set; }

        [Display(Name = "CmsFldFullDescEn")]
        public string? DescriptionEn { get; set; }

        [Display(Name = "CmsFldFullDescAr")]
        public string? DescriptionAr { get; set; }

        [Display(Name = "CmsFldMaterialIcon")]
        [StringLength(100)]
        public string? IconName { get; set; } = "psychology";

        [Display(Name = "CmsFldFeatureImage")]
        public IFormFile? ImageFile { get; set; }

        public string? ExistingImagePath { get; set; }

        [Display(Name = "CmsFldDisplayOrder")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "CmsFldIsPublished")]
        public bool IsPublished { get; set; } = true;

        public SelectList? BrandOptions { get; set; }
    }
}
