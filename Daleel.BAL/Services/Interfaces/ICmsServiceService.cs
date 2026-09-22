using Daleel.BAL.Models;

namespace Daleel.BAL.Services.Interfaces
{
    public interface ICmsServiceService
    {
        Task<PagedResult<CmsServiceListItemDto>> SearchAsync(CmsServiceQuery query);

        Task<IReadOnlyList<CmsServiceListItemDto>> GetByBrandAsync(int brandId, bool onlyPublished = false);

        Task<IReadOnlyList<CmsServiceListItemDto>> GetByBrandSlugAsync(string brandSlug, bool onlyPublished = false);

        Task<CmsServiceDetailDto?> GetByIdAsync(int id);

        Task<CmsServiceDetailDto?> GetBySlugAsync(int brandId, string slug, bool onlyPublished = false);

        Task<ServiceResult<CmsServiceDetailDto>> CreateAsync(CmsServiceInput input);

        Task<ServiceResult<CmsServiceDetailDto>> UpdateAsync(int id, CmsServiceInput input);

        Task<ServiceResult> DeleteAsync(int id);

        Task<ServiceResult<bool>> TogglePublishAsync(int id);

        Task SeedDefaultServicesIfEmptyAsync();
    }
}
