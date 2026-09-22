using System.ComponentModel.DataAnnotations;

namespace Daleel.Models
{
    /// <summary>
    /// Public "Contact Us" form. Submissions become CRM leads.
    /// </summary>
    public class ContactInquiryViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Work email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid work email address.")]
        [StringLength(254, ErrorMessage = "Email address cannot exceed 254 characters.")]
        [Display(Name = "Work Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a subject.")]
        [StringLength(100)]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(1000, MinimumLength = 3, ErrorMessage = "Message must be between 3 and 1000 characters.")]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;
    }
}
