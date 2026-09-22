using System.ComponentModel.DataAnnotations;
using Daleel.DAL.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Daleel.Models
{
    /// <summary>
    /// Bindable surface for creating/editing a deal. Id and timestamps are deliberately
    /// absent — the business layer owns those.
    /// </summary>
    public class DealFormViewModel
    {
        [Required(ErrorMessage = "Deal name is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Deal name must be between 2 and 200 characters.")]
        [Display(Name = "Deal Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Company")]
        public int? CompanyId { get; set; }

        [Display(Name = "Primary Contact")]
        public int? PrimaryContactId { get; set; }

        [Range(0, 999_999_999_999.99, ErrorMessage = "Value must be zero or more.")]
        [Display(Name = "Value")]
        public decimal Value { get; set; }

        [Required(ErrorMessage = "Currency is required.")]
        [StringLength(3, MinimumLength = 3)]
        [Display(Name = "Currency")]
        public string Currency { get; set; } = "USD";

        [Display(Name = "Stage")]
        public DealStage Stage { get; set; } = DealStage.New;

        [DataType(DataType.Date)]
        [Display(Name = "Expected Close Date")]
        public DateTime? ExpectedCloseDate { get; set; }

        [Display(Name = "Assigned To")]
        public string? AssignedToId { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        /// <summary>Populated by the controller before rendering; never posted back.</summary>
        [ValidateNever]
        public IEnumerable<SelectListItem> CompanyOptions { get; set; } = Enumerable.Empty<SelectListItem>();

        [ValidateNever]
        public IEnumerable<SelectListItem> ContactOptions { get; set; } = Enumerable.Empty<SelectListItem>();

        [ValidateNever]
        public IEnumerable<SelectListItem> AssignedToOptions { get; set; } = Enumerable.Empty<SelectListItem>();

        [ValidateNever]
        public IEnumerable<SelectListItem> CurrencyOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
