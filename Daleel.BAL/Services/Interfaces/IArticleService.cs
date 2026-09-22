using Daleel.BAL.Models;

namespace Daleel.BAL.Services.Interfaces
{
    public interface IArticleService
    {
        Task<PagedResult<ArticleListItem>> SearchAsync(ArticleQuery query);

        Task<IReadOnlyList<ArticleListItem>> GetRecentPublishedAsync(int count = 6);

        Task<ArticleDetailDto?> GetBySlugAsync(string slug, bool onlyPublished = true);

        Task<ArticleDetailDto?> GetByIdAsync(int id);

        Task<ServiceResult<ArticleDetailDto>> CreateAsync(ArticleInput input);

        Task<ServiceResult<ArticleDetailDto>> UpdateAsync(int id, ArticleInput input);

        Task<ServiceResult> DeleteAsync(int id);

        Task<ServiceResult<bool>> TogglePublishAsync(int id);

        Task<CmsDashboardStats> GetDashboardStatsAsync();

        Task<IReadOnlyList<string>> GetDistinctCategoriesAsync();

        Task<IReadOnlyList<string>> GetDistinctTagsAsync();
    }
}
