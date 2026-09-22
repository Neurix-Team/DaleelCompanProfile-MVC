namespace Daleel.DAL.Entities
{
    /// <summary>Urgency of a follow-up task.</summary>
    public enum TaskPriority
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Progress of a follow-up task. Named CrmTaskStatus rather than TaskStatus so it
    /// never collides with <see cref="System.Threading.Tasks.TaskStatus"/>.
    /// </summary>
    public enum CrmTaskStatus
    {
        Open,
        InProgress,
        Done
    }

    /// <summary>
    /// A follow-up action item. Named CrmTask rather than Task to stay clear of
    /// <see cref="System.Threading.Tasks.Task"/>. The link to a CRM record is optional —
    /// a task can stand on its own.
    /// </summary>
    public class CrmTask
    {
        public int Id { get; set; }

        public CrmEntityType? EntityType { get; set; }

        public int? EntityId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public CrmTaskStatus Status { get; set; } = CrmTaskStatus.Open;

        public string? AssignedToId { get; set; }

        public ApplicationUser? AssignedTo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>True when a task that is not finished is already past its due date.</summary>
        public bool IsOverdue =>
            Status != CrmTaskStatus.Done && DueDate.HasValue && DueDate.Value.Date < DateTime.UtcNow.Date;
    }
}
