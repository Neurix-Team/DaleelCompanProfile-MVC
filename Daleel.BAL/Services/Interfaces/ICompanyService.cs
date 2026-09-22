using Daleel.BAL.Models;
using Daleel.DAL.Entities;

namespace Daleel.BAL.Services.Interfaces
{
    /// <summary>Business operations for customer organisations.</summary>
    public interface ICompanyService
    {
        Task<PagedResult<CompanyListItem>> SearchAsync(CompanyQuery query);

        /// <summary>The company with its contacts loaded, for the detail page.</summary>
        Task<Company?> GetWithContactsAsync(int id);

        Task<Company?> GetAsync(int id);

        Task<ServiceResult<Company>> CreateAsync(CompanyInput input);

        Task<ServiceResult<Company>> UpdateAsync(int id, CompanyInput input);

        Task<ServiceResult<CompanyDeletionResult>> DeleteAsync(int id);

        /// <summary>Every company as a dropdown choice, ordered by name.</summary>
        Task<IReadOnlyList<ListOption>> GetOptionsAsync();
    }
}
