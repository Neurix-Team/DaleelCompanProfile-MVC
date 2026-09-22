using Daleel.BAL.Models;
using Daleel.DAL.Entities;

namespace Daleel.BAL.Services.Interfaces
{
    /// <summary>Business operations for follow-up tasks.</summary>
    public interface ITaskService
    {
        Task<PagedResult<TaskListItem>> SearchAsync(TaskQuery query);

        /// <summary>Tasks attached to one record, unfinished first then by due date.</summary>
        Task<IReadOnlyList<TaskListItem>> GetForEntityAsync(CrmEntityType entityType, int entityId);

        Task<CrmTask?> GetAsync(int id);

        Task<ServiceResult<CrmTask>> CreateAsync(TaskInput input);

        Task<ServiceResult<CrmTask>> UpdateAsync(int id, TaskInput input);

        /// <summary>One-click status change from a list or widget.</summary>
        Task<ServiceResult<CrmTask>> SetStatusAsync(int id, CrmTaskStatus status);

        Task<ServiceResult<string>> DeleteAsync(int id);

        /// <summary>Records of one type that a task can be linked to.</summary>
        Task<IReadOnlyList<ListOption>> GetEntityOptionsAsync(CrmEntityType entityType);
    }
}
