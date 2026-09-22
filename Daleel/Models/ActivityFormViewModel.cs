using System.ComponentModel.DataAnnotations;
using Daleel.DAL.Entities;

namespace Daleel.Models
{
    /// <summary>
    /// The inline "log an activity" form shown on every record's detail page. There is
    /// no standalone activity page, so this only ever posts.
    /// </summary>
    public class ActivityFormViewModel
    {
        public CrmEntityType EntityType { get; set; }

        public int EntityId { get; set; }

        [Display(Name = "Type")]
        public ActivityType Type { get; set; } = ActivityType.Note;

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [StringLength(4000, ErrorMessage = "Notes cannot exceed 4000 characters.")]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "When")]
        public DateTime? OccurredAt { get; set; }

        /// <summary>Where to send the user back to; validated with Url.IsLocalUrl.</summary>
        public string? ReturnUrl { get; set; }
    }
}
