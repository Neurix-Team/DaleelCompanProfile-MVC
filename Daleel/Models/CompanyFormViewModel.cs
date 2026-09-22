using System.ComponentModel.DataAnnotations;
using Daleel.DAL.Entities;

namespace Daleel.Models
{
    /// <summary>
    /// Bindable surface for creating/editing a company. Id, CreatedAt and UpdatedAt
    /// are deliberately absent — they are never accepted from the client.
    /// </summary>
    public class CompanyFormViewModel
    {
        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Company name must be between 2 and 200 characters.")]
        [Display(Name = "Company Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "Industry cannot exceed 150 characters.")]
        [Display(Name = "Industry")]
        public string? Industry { get; set; }

        [StringLength(300, ErrorMessage = "Website cannot exceed 300 characters.")]
        [Url(ErrorMessage = "Please enter a valid website URL, including http:// or https://.")]
        [Display(Name = "Website")]
        public string? Website { get; set; }

        [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [OptionalEmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(254, ErrorMessage = "Email address cannot exceed 254 characters.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters.")]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        [Display(Name = "City")]
        public string? City { get; set; }

        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters.")]
        [Display(Name = "Country")]
        public string? Country { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Status")]
        public RecordStatus Status { get; set; } = RecordStatus.Active;
    }
}
