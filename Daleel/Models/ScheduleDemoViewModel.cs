using System.ComponentModel.DataAnnotations;

namespace Daleel.Models
{
    public class ScheduleDemoViewModel
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
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$",
            ErrorMessage = "Please enter a valid work email address.")]
        [StringLength(254, ErrorMessage = "Email address cannot exceed 254 characters.")]
        [Display(Name = "Work Email")]
        public string WorkEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Company name must be between 2 and 200 characters.")]
        [Display(Name = "Company")]
        public string Company { get; set; } = string.Empty;

        [Required(ErrorMessage = "Job title is required.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Job title must be between 2 and 150 characters.")]
        [Display(Name = "Job Title")]
        public string JobTitle { get; set; } = string.Empty;
    }
}
