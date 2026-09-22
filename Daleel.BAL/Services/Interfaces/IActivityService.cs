using Daleel.BAL.Models;
using Daleel.DAL.Entities;

namespace Daleel.BAL.Services.Interfaces
{
    /// <summary>Logging and reading interactions against any CRM record.</summary>
    public interface IActivityService
    {
        /// <summary>The activity log for one record, newest interaction first.</summary>
        Task<IReadOnlyList<ActivityListItem>> GetForEntityAsync(CrmEntityType entityType, int entityId);

        Task<ServiceResult<Activity>> CreateAsync(ActivityInput input, string? createdByUserId);

        Task<ServiceResult<Activity>> DeleteAsync(int id);
    }
}
