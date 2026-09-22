using Daleel.BAL.Models;

namespace Daleel.BAL.Services.Interfaces
{
    public interface ITestimonialService
    {
        Task<PagedResult<TestimonialListItemDto>> SearchAsync(TestimonialQuery query);

        Task<IReadOnlyList<TestimonialListItemDto>> GetByBrandAsync(int brandId, bool onlyPublished = false);

        Task<IReadOnlyList<TestimonialListItemDto>> GetByBrandSlugAsync(string brandSlug, bool onlyPublished = false);

        Task<TestimonialDetailDto?> GetByIdAsync(int id);

        Task<ServiceResult<TestimonialDetailDto>> CreateAsync(TestimonialInput input);

        Task<ServiceResult<TestimonialDetailDto>> UpdateAsync(int id, TestimonialInput input);

        Task<ServiceResult> DeleteAsync(int id);

        Task<ServiceResult<bool>> TogglePublishAsync(int id);

        Task SeedDefaultTestimonialsIfEmptyAsync();
    }
}
