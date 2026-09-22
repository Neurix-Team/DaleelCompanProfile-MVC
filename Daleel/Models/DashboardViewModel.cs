using Daleel.BAL.Models;

namespace Daleel.Models
{
    /// <summary>Everything the CRM landing page renders, assembled by the controller.</summary>
    public class DashboardViewModel
    {
        public DashboardKpis Kpis { get; init; } = new();

        public IReadOnlyList<CurrencyTotal> Pipeline { get; init; } = Array.Empty<CurrencyTotal>();

        public IReadOnlyList<TaskListItem> UpcomingTasks { get; init; } = Array.Empty<TaskListItem>();

        public IReadOnlyList<ActivityListItem> RecentActivities { get; init; } = Array.Empty<ActivityListItem>();
    }
}
