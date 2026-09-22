namespace Daleel.DAL.Entities
{
    /// <summary>
    /// A person. Optionally linked to a <see cref="Company"/> — a contact may exist
    /// on its own before we know which organisation they belong to.
    /// </summary>
    public class Contact
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? JobTitle { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public int? CompanyId { get; set; }

        public Company? Company { get; set; }

        public RecordStatus Status { get; set; } = RecordStatus.Active;

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
