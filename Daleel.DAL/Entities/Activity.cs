namespace Daleel.DAL.Entities
{
    /// <summary>
    /// A logged interaction — a call, email, meeting or plain note — against a lead,
    /// contact, company or deal. Immutable once written apart from deletion: an
    /// activity is a record of what happened, not a working document.
    /// </summary>
    public class Activity
    {
        public int Id { get; set; }

        public CrmEntityType EntityType { get; set; }

        public int EntityId { get; set; }

        public ActivityType Type { get; set; } = ActivityType.Note;

        public string Subject { get; set; } = string.Empty;

        public string? Notes { get; set; }

        /// <summary>
        /// When the interaction actually happened — editable, because staff routinely
        /// log yesterday's call this morning.
        /// </summary>
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        public string? CreatedByUserId { get; set; }

        public ApplicationUser? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
