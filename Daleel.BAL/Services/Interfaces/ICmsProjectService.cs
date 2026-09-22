using Daleel.BAL.Models;

namespace Daleel.BAL.Services.Interfaces
{
    public interface ICmsProjectService
    {
        Task<PagedResult<CmsProjectListItemDto>> SearchAsync(CmsProjectQuery query);

        Task<IReadOnlyList<CmsProjectListItemDto>> GetByBrandAsync(int brandId, bool onlyPublished = false);

        Task<IReadOnlyList<CmsProjectListItemDto>> GetByBrandSlugAsync(string brandSlug, bool onlyPublished = false);

        Task<CmsProjectDetailDto?> GetByIdAsync(int id);

        Task<CmsProjectDetailDto?> GetBySlugAsync(int brandId, string slug, bool onlyPublished = false);

        Task<ServiceResult<CmsProjectDetailDto>> CreateAsync(CmsProjectInput input);

        Task<ServiceResult<CmsProjectDetailDto>> UpdateAsync(int id, CmsProjectInput input);

        Task<ServiceResult> DeleteAsync(int id);

        Task<ServiceResult<bool>> TogglePublishAsync(int id);

        Task SeedDefaultProjectsIfEmptyAsync();
    }
}
