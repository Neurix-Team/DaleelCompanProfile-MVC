using Daleel.BAL.Models;

namespace Daleel.BAL.Services.Interfaces
{
    public interface ITeamMemberService
    {
        Task<PagedResult<TeamMemberListItemDto>> SearchAsync(TeamMemberQuery query);

        Task<IReadOnlyList<TeamMemberListItemDto>> GetByBrandAsync(int brandId, bool onlyPublished = false);

        Task<IReadOnlyList<TeamMemberListItemDto>> GetByBrandSlugAsync(string brandSlug, bool onlyPublished = false);

        Task<TeamMemberDetailDto?> GetByIdAsync(int id);

        Task<TeamMemberDetailDto?> GetBySlugAsync(int brandId, string slug, bool onlyPublished = false);

        Task<ServiceResult<TeamMemberDetailDto>> CreateAsync(TeamMemberInput input);

        Task<ServiceResult<TeamMemberDetailDto>> UpdateAsync(int id, TeamMemberInput input);

        Task<ServiceResult> DeleteAsync(int id);

        Task<ServiceResult<bool>> TogglePublishAsync(int id);

        Task SeedDefaultTeamIfEmptyAsync();
    }
}
