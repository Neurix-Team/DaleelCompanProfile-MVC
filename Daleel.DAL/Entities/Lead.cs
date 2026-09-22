namespace Daleel.DAL.Entities
{
    /// <summary>
    /// A potential customer who has not yet been converted. Company and job details
    /// are free text at this stage — a lead is captured before we know whether it maps
    /// to a real <see cref="Company"/> or <see cref="Contact"/> record. That linking
    /// happens during conversion.
    /// </summary>
    public class Lead
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? CompanyName { get; set; }

        public string? JobTitle { get; set; }

        public LeadStatus Status { get; set; } = LeadStatus.New;

        public LeadSource Source { get; set; } = LeadSource.Website;

        public string? AssignedToId { get; set; }

        public ApplicationUser? AssignedTo { get; set; }

        public string? Notes { get; set; }

        /// <summary>Archived leads stay in the database but drop out of the default list.</summary>
        public bool IsArchived { get; set; }

        /// <summary>
        /// The contact this lead became. Set once, at conversion, and kept afterwards so
        /// the lead remains an auditable record of where that contact came from.
        /// </summary>
        public int? ConvertedContactId { get; set; }

        public Contact? ConvertedContact { get; set; }

        public DateTime? ConvertedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
