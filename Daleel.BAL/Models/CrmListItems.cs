using Daleel.DAL.Entities;

namespace Daleel.BAL.Models
{
    /// <summary>
    /// A choice for a dropdown. Deliberately not MVC's SelectListItem — the business
    /// layer stays free of any presentation-framework types.
    /// </summary>
    public sealed record ListOption(string Value, string Text);

    /// <summary>
    /// Flat row for the company list. Projected in the query so the contact count
    /// comes back as a COUNT rather than by loading every related contact.
    /// </summary>
    public class CompanyListItem
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Industry { get; init; }
        public string? City { get; init; }
        public string? Country { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public RecordStatus Status { get; init; }
        public int ContactCount { get; init; }

        public string? Location =>
            string.Join(", ", new[] { City, Country }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    /// <summary>Flat row for the contact list.</summary>
    public class ContactListItem
    {
        public int Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? JobTitle { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public int? CompanyId { get; init; }
        public string? CompanyName { get; init; }
        public RecordStatus Status { get; init; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    /// <summary>Flat row for the deal list.</summary>
    public class DealListItem
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int? CompanyId { get; init; }
        public string? CompanyName { get; init; }
        public string? PrimaryContactName { get; init; }
        public decimal Value { get; init; }
        public string Currency { get; init; } = "USD";
        public DealStage Stage { get; init; }
        public DateTime? ExpectedCloseDate { get; init; }
        public string? AssignedToName { get; init; }
        public DateTime CreatedAt { get; init; }

        public bool IsClosed => Stage is DealStage.Won or DealStage.Lost;

        /// <summary>True when an open deal's expected close date has already passed.</summary>
        public bool IsOverdue =>
            !IsClosed && ExpectedCloseDate.HasValue && ExpectedCloseDate.Value.Date < DateTime.UtcNow.Date;
    }

    /// <summary>A single card on the pipeline board.</summary>
    public sealed record PipelineCard(
        int Id,
        string Name,
        string? CompanyName,
        decimal Value,
        string Currency,
        DateTime? ExpectedCloseDate,
        string? AssignedToName);

    /// <summary>One stage column of the pipeline board, with its own roll-up.</summary>
    public sealed record PipelineColumn(
        DealStage Stage,
        IReadOnlyList<PipelineCard> Cards)
    {
        public int Count => Cards.Count;

        public decimal TotalValue => Cards.Sum(c => c.Value);
    }

    /// <summary>
    /// The whole board. Every stage is present even when empty so the columns stay
    /// stable as deals move.
    /// </summary>
    public sealed class PipelineBoard
    {
        public IReadOnlyList<PipelineColumn> Columns { get; init; } = Array.Empty<PipelineColumn>();

        /// <summary>Currency shown on the roll-ups; null when deals mix currencies.</summary>
        public string? Currency { get; init; }

        public bool IsMixedCurrency { get; init; }

        public int TotalCount => Columns.Sum(c => c.Count);

        /// <summary>Value of deals still in play — excludes Won and Lost.</summary>
        public decimal OpenValue => Columns
            .Where(c => c.Stage is not DealStage.Won and not DealStage.Lost)
            .Sum(c => c.TotalValue);
    }

    /// <summary>One entry in a record's activity log.</summary>
    public class ActivityListItem
    {
        public int Id { get; init; }
        public ActivityType Type { get; init; }
        public string Subject { get; init; } = string.Empty;
        public string? Notes { get; init; }
        public DateTime OccurredAt { get; init; }
        public string? CreatedByName { get; init; }
        public CrmEntityType EntityType { get; init; }
        public int EntityId { get; init; }
    }

    /// <summary>Flat row for the task list.</summary>
    public class TaskListItem
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public DateTime? DueDate { get; init; }
        public TaskPriority Priority { get; init; }
        public CrmTaskStatus Status { get; init; }
        public string? AssignedToName { get; init; }
        public CrmEntityType? EntityType { get; init; }
        public int? EntityId { get; init; }

        /// <summary>Display name of the linked record, resolved by the service.</summary>
        public string? EntityLabel { get; set; }

        public bool IsOverdue =>
            Status != CrmTaskStatus.Done && DueDate.HasValue && DueDate.Value.Date < DateTime.UtcNow.Date;
    }

    /// <summary>Headline counts on the dashboard.</summary>
    public class DashboardKpis
    {
        public int TotalLeads { get; init; }
        public int NewLeads { get; init; }
        public int QualifiedLeads { get; init; }
        public int ConvertedLeads { get; init; }
        public int TotalCustomers { get; init; }
        public int TotalCompanies { get; init; }
        public int OpenDeals { get; init; }
        public int WonDeals { get; init; }
        public int LostDeals { get; init; }
        public int OpenTasks { get; init; }
        public int OverdueTasks { get; init; }
    }

    /// <summary>
    /// Open pipeline value for one currency. Kept per-currency because summing across
    /// currencies without conversion rates would be a made-up number.
    /// </summary>
    public sealed record CurrencyTotal(string Currency, decimal Total, int Count);

    /// <summary>Flat row for the lead list.</summary>
    public class LeadListItem
    {
        public int Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? CompanyName { get; init; }
        public LeadStatus Status { get; init; }
        public LeadSource Source { get; init; }
        public string? AssignedToName { get; init; }
        public bool IsArchived { get; init; }
        public DateTime CreatedAt { get; init; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
