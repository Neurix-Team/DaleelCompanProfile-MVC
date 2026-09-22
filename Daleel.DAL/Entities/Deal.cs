namespace Daleel.DAL.Entities
{
    /// <summary>
    /// A sales opportunity moving through the pipeline. Company and primary contact are
    /// optional so a deal can be opened before those records catch up.
    /// </summary>
    public class Deal
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int? CompanyId { get; set; }

        public Company? Company { get; set; }

        public int? PrimaryContactId { get; set; }

        public Contact? PrimaryContact { get; set; }

        public decimal Value { get; set; }

        public string Currency { get; set; } = "USD";

        public DealStage Stage { get; set; } = DealStage.New;

        public DateTime? ExpectedCloseDate { get; set; }

        public string? AssignedToId { get; set; }

        public ApplicationUser? AssignedTo { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Won and Lost are terminal — they leave the open pipeline.</summary>
        public bool IsClosed => Stage is DealStage.Won or DealStage.Lost;
    }
}
