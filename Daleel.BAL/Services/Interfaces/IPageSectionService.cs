using Daleel.BAL.Models;

namespace Daleel.BAL.Services.Interfaces
{
    public interface IPageSectionService
    {
        Task<IReadOnlyList<PageSectionDto>> GetSectionsByPageAsync(string pageKey);

        Task<IReadOnlyDictionary<string, string>> GetPageContentMapAsync(string pageKey, string culture = "en");

        Task<PageSectionDto?> GetAsync(string pageKey, string sectionKey);

        Task<IReadOnlyList<string>> GetDistinctPagesAsync();

        Task<ServiceResult> UpsertAsync(PageSectionInput input);

        Task<ServiceResult> BulkUpsertAsync(string pageKey, IEnumerable<PageSectionInput> inputs);

        Task<ServiceResult> DeleteSectionAsync(string pageKey, string sectionKey);

        Task SeedMissingDefaultSectionsAsync();

        /// <summary>
        /// Rewrites original-four-page rows that still hold the never-rendered placeholder copy.
        /// Returns how many rows were changed. Admin-edited rows are never touched.
        /// </summary>
        Task<int> AlignLegacyPlaceholderSectionsAsync();

        /// <summary>
        /// Inserts any missing default section keys for existing or new pages without touching existing rows.
        /// </summary>
        Task<int> SyncMissingSectionKeysAsync();
    }
}
