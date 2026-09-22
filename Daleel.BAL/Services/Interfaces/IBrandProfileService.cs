using Daleel.BAL.Models;

namespace Daleel.BAL.Services.Interfaces
{
    public interface IBrandProfileService
    {
        Task<IReadOnlyList<BrandProfileListItemDto>> GetAllAsync(bool onlyPublished = false);

        Task<BrandProfileDetailDto?> GetByIdAsync(int id);

        Task<BrandProfileDetailDto?> GetBySlugAsync(string slug, bool onlyPublished = false);

        Task<ServiceResult<BrandProfileDetailDto>> CreateAsync(BrandProfileInput input);

        Task<ServiceResult<BrandProfileDetailDto>> UpdateAsync(int id, BrandProfileInput input);

        Task<ServiceResult> DeleteAsync(int id);

        Task<ServiceResult<bool>> TogglePublishAsync(int id);

        Task SeedDefaultBrandsIfEmptyAsync();
    }
}
