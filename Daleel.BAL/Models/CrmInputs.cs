using Daleel.DAL.Entities;

namespace Daleel.BAL.Models
{
    /// <summary>
    /// Fields accepted when creating or updating a company. Ids and timestamps are
    /// deliberately absent — the service owns those.
    /// </summary>
    public class CompanyInput
    {
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
    }

    /// <summary>Fields accepted when creating or updating a contact.</summary>
    public class ContactInput
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int? CompanyId { get; set; }
        public RecordStatus Status { get; set; } = RecordStatus.Active;
        public string? Notes { get; set; }
    }

    /// <summary>Fields accepted when a staff member creates or updates a lead.</summary>
    public class LeadInput
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? CompanyName { get; set; }
        public string? JobTitle { get; set; }
        public LeadStatus Status { get; set; } = LeadStatus.New;
        public LeadSource Source { get; set; } = LeadSource.Website;
        public string? AssignedToId { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// A lead arriving from a public marketing form. Narrower than <see cref="LeadInput"/>:
    /// a website visitor never gets to choose status or assignment.
    /// </summary>
    public class LeadCaptureInput
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? CompanyName { get; set; }
        public string? JobTitle { get; set; }
        public LeadSource Source { get; set; } = LeadSource.Website;
        public string? Notes { get; set; }
    }

    /// <summary>Fields accepted when creating or updating a deal.</summary>
    public class DealInput
    {
        public string Name { get; set; } = string.Empty;
        public int? CompanyId { get; set; }
        public int? PrimaryContactId { get; set; }
        public decimal Value { get; set; }
        public string Currency { get; set; } = "USD";
        public DealStage Stage { get; set; } = DealStage.New;
        public DateTime? ExpectedCloseDate { get; set; }
        public string? AssignedToId { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>Fields accepted when logging an activity against a record.</summary>
    public class ActivityInput
    {
        public CrmEntityType EntityType { get; set; }
        public int EntityId { get; set; }
        public ActivityType Type { get; set; } = ActivityType.Note;
        public string Subject { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime? OccurredAt { get; set; }
    }

    /// <summary>Fields accepted when creating or updating a follow-up task.</summary>
    public class TaskInput
    {
        public CrmEntityType? EntityType { get; set; }
        public int? EntityId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DueDate { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public CrmTaskStatus Status { get; set; } = CrmTaskStatus.Open;
        public string? AssignedToId { get; set; }
    }

    /// <summary>Choices made on the lead conversion form.</summary>
    public class LeadConversionInput
    {
        /// <summary>Attach the lead to <see cref="ExistingContactId"/> instead of creating a contact.</summary>
        public bool LinkExistingContact { get; set; }

        public int? ExistingContactId { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? JobTitle { get; set; }

        /// <summary>Create a company from <see cref="NewCompanyName"/> rather than linking one.</summary>
        public bool CreateNewCompany { get; set; }

        public int? ExistingCompanyId { get; set; }

        public string? NewCompanyName { get; set; }
    }
}
