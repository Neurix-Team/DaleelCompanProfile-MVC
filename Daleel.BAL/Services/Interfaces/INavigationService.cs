using Daleel.BAL.Models;
using Daleel.DAL.Entities;

namespace Daleel.BAL.Services.Interfaces
{
    public interface INavigationService
    {
        Task<IReadOnlyList<NavigationLinkDto>> GetAllAsync(NavigationLinkQuery? query = null);

        Task<NavigationLinkDto?> GetByIdAsync(int id);

        Task<ServiceResult<NavigationLinkDto>> CreateAsync(NavigationLinkInput input);

        Task<ServiceResult<NavigationLinkDto>> UpdateAsync(int id, NavigationLinkInput input);

        Task<ServiceResult<bool>> DeleteAsync(int id);

        Task<ServiceResult<bool>> ToggleActiveAsync(int id);

        Task<ServiceResult<bool>> UpdateSortOrderAsync(int id, int newSortOrder);

        Task<ServiceResult<bool>> ReorderAsync(IReadOnlyList<int> orderedIds);

        Task<IReadOnlyList<NavigationLinkDto>> GetActiveHeaderLinksAsync();

        Task<IReadOnlyDictionary<FooterSection, IReadOnlyList<NavigationLinkDto>>> GetActiveFooterLinksGroupedAsync();

        Task<GroupedNavigationDto> GetGroupedNavigationAsync();

        Task SeedDefaultLinksIfEmptyAsync();
    }
}
