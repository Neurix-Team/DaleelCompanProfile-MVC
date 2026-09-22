namespace Daleel.DAL.Entities
{
    /// <summary>
    /// A customer organisation. One company can hold many contacts.
    /// </summary>
    public class Company
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Industry { get; set; }

        public string? Website { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? Address { get; set; }

        public string? City { get; set; }

        public string? Country { get; set; }

        public string? Description { get; set; }

        public RecordStatus Status { get; set; } = RecordStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
    }
}
