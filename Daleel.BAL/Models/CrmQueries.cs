using Daleel.DAL.Entities;

namespace Daleel.BAL.Models
{
    /// <summary>Shared paging parameters. Page is clamped by the services, not the caller.</summary>
    public abstract class PagedQuery
    {
        public const int DefaultPageSize = 10;

        public string? Q { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = DefaultPageSize;
    }

    public class CompanyQuery : PagedQuery
    {
        public RecordStatus? Status { get; set; }
    }

    public class ContactQuery : PagedQuery
    {
        public RecordStatus? Status { get; set; }

        public int? CompanyId { get; set; }
    }

    public class LeadQuery : PagedQuery
    {
        public LeadStatus? Status { get; set; }

        public LeadSource? Source { get; set; }

        public string? AssignedToId { get; set; }

        public bool ShowArchived { get; set; }
    }

    /// <summary>How the deal list is ordered.</summary>
    public enum DealSort
    {
        Newest,
        Name,
        ValueHigh,
        ValueLow,
        CloseDate
    }

    public class DealQuery : PagedQuery
    {
        public DealStage? Stage { get; set; }

        public int? CompanyId { get; set; }

        public string? AssignedToId { get; set; }

        /// <summary>Hides Won/Lost deals when true.</summary>
        public bool OpenOnly { get; set; }

        public DealSort Sort { get; set; } = DealSort.Newest;
    }

    public class TaskQuery : PagedQuery
    {
        public CrmTaskStatus? Status { get; set; }

        public string? AssignedToId { get; set; }

        /// <summary>Hides finished tasks when true.</summary>
        public bool OpenOnly { get; set; }

        /// <summary>Only unfinished tasks whose due date has already passed.</summary>
        public bool OverdueOnly { get; set; }
    }

    /// <summary>
    /// What happened when a company was deleted. The orphan count is business data —
    /// the caller turns it into a message.
    /// </summary>
    public sealed record CompanyDeletionResult(string Name, int OrphanedContacts);

    /// <summary>The outcome of converting a lead.</summary>
    /// <param name="LinkedExisting">True when an existing contact was reused rather than created.</param>
    public sealed record LeadConversionResult(
        int ContactId,
        string ContactName,
        string LeadName,
        bool LinkedExisting);

    /// <summary>Everything the conversion form needs to render for a given lead.</summary>
    public sealed record LeadConversionContext(
        int LeadId,
        string LeadName,
        string FirstName,
        string LastName,
        string? Email,
        string? Phone,
        string? JobTitle,
        ContactListItem? DuplicateContact,
        int? MatchedCompanyId,
        string? UnmatchedCompanyName);
}
