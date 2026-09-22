using Daleel.BAL.Models;
using Daleel.DAL.Entities;

namespace Daleel.BAL.Services.Interfaces
{
    /// <summary>Business operations for leads, including conversion into a contact.</summary>
    public interface ILeadService
    {
        Task<PagedResult<LeadListItem>> SearchAsync(LeadQuery query);

        /// <summary>The lead with its assignee and converted contact loaded, for the detail page.</summary>
        Task<Lead?> GetWithRelationsAsync(int id);

        Task<Lead?> GetAsync(int id);

        Task<ServiceResult<Lead>> CreateAsync(LeadInput input);

        Task<ServiceResult<Lead>> UpdateAsync(int id, LeadInput input);

        Task<ServiceResult<Lead>> SetArchivedAsync(int id, bool archived);

        /// <summary>
        /// Records a lead submitted through a public marketing form. Returns false when
        /// persistence failed — capture is best-effort so a website visitor never sees
        /// an error page because the CRM database is momentarily unavailable.
        /// </summary>
        Task<bool> CaptureAsync(LeadCaptureInput input);

        /// <summary>
        /// Everything the conversion form needs, including the duplicate-contact and
        /// existing-company matches. Fails when the lead cannot be converted.
        /// </summary>
        Task<ServiceResult<LeadConversionContext>> GetConversionContextAsync(int id);

        Task<ServiceResult<LeadConversionResult>> ConvertAsync(int id, LeadConversionInput input);

        /// <summary>Staff accounts a lead can be assigned to, as dropdown choices.</summary>
        Task<IReadOnlyList<ListOption>> GetAssignableUsersAsync();
    }
}
