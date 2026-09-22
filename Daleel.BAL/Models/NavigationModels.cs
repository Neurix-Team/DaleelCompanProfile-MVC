using System.ComponentModel.DataAnnotations;
using Daleel.DAL.Entities;

namespace Daleel.BAL.Models
{
    public class NavigationLinkDto
    {
        public int Id { get; set; }

        public NavigationLocation Location { get; set; }

        public FooterSection? Section { get; set; }

        public string LabelEn { get; set; } = string.Empty;

        public string LabelAr { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public bool IsActive { get; set; }

        public bool OpenInNewTab { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    public class NavigationLinkInput
    {
        [Required(ErrorMessage = "Location is required.")]
        public NavigationLocation Location { get; set; } = NavigationLocation.Header;

        public FooterSection? Section { get; set; }

        [Required(ErrorMessage = "English label is required.")]
        [MaxLength(100, ErrorMessage = "English label cannot exceed 100 characters.")]
        public string LabelEn { get; set; } = string.Empty;

        [Required(ErrorMessage = "Arabic label is required.")]
        [MaxLength(100, ErrorMessage = "Arabic label cannot exceed 100 characters.")]
        public string LabelAr { get; set; } = string.Empty;

        [Required(ErrorMessage = "URL / path is required.")]
        [MaxLength(500, ErrorMessage = "URL cannot exceed 500 characters.")]
        public string Url { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public bool OpenInNewTab { get; set; } = false;
    }

    public class NavigationLinkQuery
    {
        public NavigationLocation? Location { get; set; }

        public FooterSection? Section { get; set; }

        public bool? IsActive { get; set; }

        public string? Q { get; set; }
    }

    public class GroupedNavigationDto
    {
        public IReadOnlyList<NavigationLinkDto> HeaderLinks { get; set; } = Array.Empty<NavigationLinkDto>();

        public IReadOnlyList<NavigationLinkDto> FooterPlatformLinks { get; set; } = Array.Empty<NavigationLinkDto>();

        public IReadOnlyList<NavigationLinkDto> FooterCompanyLinks { get; set; } = Array.Empty<NavigationLinkDto>();

        public IReadOnlyList<NavigationLinkDto> FooterStayInformedLinks { get; set; } = Array.Empty<NavigationLinkDto>();
    }
}
