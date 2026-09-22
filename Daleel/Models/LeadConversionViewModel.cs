using System.ComponentModel.DataAnnotations;
using Daleel.BAL.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Daleel.Models
{
    /// <summary>
    /// Bindable surface for turning a lead into a contact. The contact fields are
    /// pre-filled from the lead but stay editable — conversion is the moment the
    /// details get cleaned up before they become a permanent record.
    /// </summary>
    public class LeadConversionViewModel
    {
        /// <summary>
        /// Sentinel <see cref="ExistingCompanyId"/> value meaning "create a company from
        /// <see cref="NewCompanyName"/>". Numeric so the option value is culture-independent,
        /// and 0 is never a real identity value.
        /// </summary>
        public const int CreateNewCompanyId = 0;

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [OptionalEmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(254, ErrorMessage = "Email address cannot exceed 254 characters.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [StringLength(150, ErrorMessage = "Job title cannot exceed 150 characters.")]
        [Display(Name = "Job Title")]
        public string? JobTitle { get; set; }

        /// <summary>
        /// When true the lead is attached to <see cref="ExistingContactId"/> instead of
        /// creating a second record for the same person.
        /// </summary>
        [Display(Name = "Link to the existing contact instead of creating a new one")]
        public bool LinkExistingContact { get; set; }

        public int? ExistingContactId { get; set; }

        /// <summary>
        /// Null = no company, <see cref="CreateNewCompanyId"/> = create one from
        /// <see cref="NewCompanyName"/>, anything else = an existing company's id.
        /// </summary>
        [Display(Name = "Company")]
        public int? ExistingCompanyId { get; set; }

        [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters.")]
        [Display(Name = "New Company Name")]
        public string? NewCompanyName { get; set; }

        /// <summary>Populated by the controller before rendering; never posted back.</summary>
        [ValidateNever]
        public IEnumerable<SelectListItem> CompanyOptions { get; set; } = Enumerable.Empty<SelectListItem>();

        /// <summary>An existing contact sharing this lead's email, if there is one.</summary>
        [ValidateNever]
        public ContactListItem? DuplicateContact { get; set; }

        /// <summary>Read-only lead context shown at the top of the form.</summary>
        [ValidateNever]
        public string LeadName { get; set; } = string.Empty;
    }
}
