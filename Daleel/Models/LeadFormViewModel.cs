using System.ComponentModel.DataAnnotations;
using Daleel.DAL.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Daleel.Models
{
    /// <summary>
    /// Bindable surface for creating/editing a lead. Id, CreatedAt, UpdatedAt and
    /// IsArchived are deliberately absent — archiving has its own action.
    /// </summary>
    public class LeadFormViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        // Optional: a lead may arrive with only a phone number.
        [OptionalEmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(254, ErrorMessage = "Email address cannot exceed 254 characters.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters.")]
        [Display(Name = "Company")]
        public string? CompanyName { get; set; }

        [StringLength(150, ErrorMessage = "Job title cannot exceed 150 characters.")]
        [Display(Name = "Job Title")]
        public string? JobTitle { get; set; }

        [Display(Name = "Status")]
        public LeadStatus Status { get; set; } = LeadStatus.New;

        [Display(Name = "Source")]
        public LeadSource Source { get; set; } = LeadSource.Website;

        [Display(Name = "Assigned To")]
        public string? AssignedToId { get; set; }

        [StringLength(4000, ErrorMessage = "Notes cannot exceed 4000 characters.")]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        /// <summary>Populated by the controller before rendering; never posted back.</summary>
        [ValidateNever]
        public IEnumerable<SelectListItem> AssignedToOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
