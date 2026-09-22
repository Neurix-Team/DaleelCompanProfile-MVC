using System.ComponentModel.DataAnnotations;
using Daleel.BAL.Models;
using Daleel.DAL.Entities;

namespace Daleel.Models
{
    public class NavigationIndexViewModel
    {
        public GroupedNavigationDto Navigation { get; set; } = new();

        public string ActiveTab { get; set; } = "header";

        public int TotalCount => Navigation.HeaderLinks.Count +
                                 Navigation.FooterPlatformLinks.Count +
                                 Navigation.FooterCompanyLinks.Count +
                                 Navigation.FooterStayInformedLinks.Count;
    }

    public class NavigationFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        [Display(Name = "Location")]
        public NavigationLocation Location { get; set; } = NavigationLocation.Header;

        [Display(Name = "Footer Column / Section")]
        public FooterSection? Section { get; set; }

        [Required(ErrorMessage = "English label is required.")]
        [MaxLength(100, ErrorMessage = "English label cannot exceed 100 characters.")]
        [Display(Name = "English Label")]
        public string LabelEn { get; set; } = string.Empty;

        [Required(ErrorMessage = "Arabic label is required.")]
        [MaxLength(100, ErrorMessage = "Arabic label cannot exceed 100 characters.")]
        [Display(Name = "Arabic Label")]
        public string LabelAr { get; set; } = string.Empty;

        [Required(ErrorMessage = "URL / Path is required.")]
        [MaxLength(500, ErrorMessage = "URL cannot exceed 500 characters.")]
        [Display(Name = "URL / Target Path")]
        public string Url { get; set; } = string.Empty;

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; } = 0;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Open in New Tab")]
        public bool OpenInNewTab { get; set; } = false;

        public bool IsEdit => Id.HasValue && Id.Value > 0;
    }
}
