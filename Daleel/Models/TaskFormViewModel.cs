using System.ComponentModel.DataAnnotations;
using Daleel.DAL.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Daleel.Models
{
    /// <summary>
    /// Bindable surface for creating/editing a follow-up task. The record link is a
    /// (type, id) pair rather than four separate fields, matching the entity.
    /// </summary>
    public class TaskFormViewModel
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 200 characters.")]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Priority")]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        [Display(Name = "Status")]
        public CrmTaskStatus Status { get; set; } = CrmTaskStatus.Open;

        [Display(Name = "Assigned To")]
        public string? AssignedToId { get; set; }

        [Display(Name = "Related To")]
        public CrmEntityType? EntityType { get; set; }

        [Display(Name = "Record")]
        public int? EntityId { get; set; }

        /// <summary>Populated by the controller before rendering; never posted back.</summary>
        [ValidateNever]
        public IEnumerable<SelectListItem> AssignedToOptions { get; set; } = Enumerable.Empty<SelectListItem>();

        /// <summary>Record options keyed by entity type, so the form can swap lists client-side.</summary>
        [ValidateNever]
        public IDictionary<CrmEntityType, IEnumerable<SelectListItem>> EntityOptions { get; set; }
            = new Dictionary<CrmEntityType, IEnumerable<SelectListItem>>();

        /// <summary>Where to send the user back to after a quick-add; validated with Url.IsLocalUrl.</summary>
        public string? ReturnUrl { get; set; }
    }
}
