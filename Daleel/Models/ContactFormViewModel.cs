using System.ComponentModel.DataAnnotations;
using Daleel.DAL.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Daleel.Models
{
    /// <summary>
    /// Bindable surface for creating/editing a contact. Id, CreatedAt and UpdatedAt
    /// are deliberately absent — they are never accepted from the client.
    /// </summary>
    public class ContactFormViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "Job title cannot exceed 150 characters.")]
        [Display(Name = "Job Title")]
        public string? JobTitle { get; set; }

        [OptionalEmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(254, ErrorMessage = "Email address cannot exceed 254 characters.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [Display(Name = "Company")]
        public int? CompanyId { get; set; }

        [Display(Name = "Status")]
        public RecordStatus Status { get; set; } = RecordStatus.Active;

        [StringLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters.")]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        /// <summary>Populated by the controller before rendering; never posted back.</summary>
        [ValidateNever]
        public IEnumerable<SelectListItem> CompanyOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
