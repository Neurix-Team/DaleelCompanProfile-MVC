using System.ComponentModel.DataAnnotations;
using Daleel.BAL.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Daleel.Models
{
    public class CmsProjectFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a brand profile.")]
        [Display(Name = "CmsFldBrandCompany")]
        public int BrandId { get; set; }

        public string? Slug { get; set; }

        [Required(ErrorMessage = "English project name is required.")]
        [Display(Name = "CmsFldProjectNameEn")]
        [StringLength(200)]
        public string NameEn { get; set; } = string.Empty;

        [Required(ErrorMessage = "Arabic project name is required.")]
        [Display(Name = "CmsFldProjectNameAr")]
        [StringLength(200)]
        public string NameAr { get; set; } = string.Empty;

        [Display(Name = "CmsFldShortSummaryEn")]
        [StringLength(500)]
        public string? ShortDescEn { get; set; }

        [Display(Name = "CmsFldShortSummaryAr")]
        [StringLength(500)]
        public string? ShortDescAr { get; set; }

        [Display(Name = "CmsFldCaseStudyEn")]
        public string? DescriptionEn { get; set; }

        [Display(Name = "CmsFldCaseStudyAr")]
        public string? DescriptionAr { get; set; }

        [Display(Name = "CmsFldClientNameEn")]
        [StringLength(200)]
        public string? ClientNameEn { get; set; }

        [Display(Name = "CmsFldClientNameAr")]
        [StringLength(200)]
        public string? ClientNameAr { get; set; }

        [Display(Name = "CmsFldCompletionDate")]
        [DataType(DataType.Date)]
        public DateTime? CompletionDate { get; set; }

        [Display(Name = "CmsFldLiveDemoUrl")]
        [StringLength(500)]
        [Url(ErrorMessage = "Enter a valid URL.")]
        public string? ProjectUrl { get; set; }

        [Display(Name = "CmsFldProjectCover")]
        public IFormFile? ImageFile { get; set; }

        public string? ExistingImagePath { get; set; }

        [Display(Name = "CmsFldDisplayOrder")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "CmsFldIsPublished")]
        public bool IsPublished { get; set; } = true;

        public SelectList? BrandOptions { get; set; }
    }
}
