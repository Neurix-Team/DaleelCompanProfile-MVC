using Daleel.BAL.Models;

namespace Daleel.BAL.Services.Interfaces
{
    /// <summary>Read-only roll-ups for the CRM landing page.</summary>
    public interface IDashboardService
    {
        Task<DashboardKpis> GetKpisAsync();

        /// <summary>Open pipeline value, one row per currency in use.</summary>
        Task<IReadOnlyList<CurrencyTotal>> GetPipelineByCurrencyAsync();

        /// <summary>Unfinished tasks with the nearest due dates; undated tasks last.</summary>
        Task<IReadOnlyList<TaskListItem>> GetUpcomingTasksAsync(int take = 5);

        Task<IReadOnlyList<ActivityListItem>> GetRecentActivitiesAsync(int take = 5);
    }
}
