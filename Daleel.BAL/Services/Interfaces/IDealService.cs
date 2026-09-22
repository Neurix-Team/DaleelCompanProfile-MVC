using Daleel.BAL.Models;
using Daleel.DAL.Entities;

namespace Daleel.BAL.Services.Interfaces
{
    /// <summary>Business operations for deals and the sales pipeline board.</summary>
    public interface IDealService
    {
        Task<PagedResult<DealListItem>> SearchAsync(DealQuery query);

        /// <summary>Every deal grouped into stage columns for the Kanban view.</summary>
        Task<PipelineBoard> GetPipelineAsync(string? assignedToId = null);

        /// <summary>The deal with company, contact and assignee loaded, for the detail page.</summary>
        Task<Deal?> GetWithRelationsAsync(int id);

        Task<Deal?> GetAsync(int id);

        Task<ServiceResult<Deal>> CreateAsync(DealInput input);

        Task<ServiceResult<Deal>> UpdateAsync(int id, DealInput input);

        /// <summary>Moves a deal to another stage. Used by the pipeline board.</summary>
        Task<ServiceResult<Deal>> MoveToStageAsync(int id, DealStage stage);

        Task<ServiceResult<string>> DeleteAsync(int id);

        /// <summary>Contacts that can be a primary contact, optionally limited to one company.</summary>
        Task<IReadOnlyList<ListOption>> GetContactOptionsAsync(int? companyId = null);

        /// <summary>The currency codes offered on the deal form.</summary>
        IReadOnlyList<string> GetCurrencies();
    }
}
