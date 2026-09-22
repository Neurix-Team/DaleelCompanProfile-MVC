using Daleel.BAL.Models;
using Daleel.DAL.Entities;

namespace Daleel.BAL.Services.Interfaces
{
    /// <summary>Business operations for people.</summary>
    public interface IContactService
    {
        Task<PagedResult<ContactListItem>> SearchAsync(ContactQuery query);

        /// <summary>The contact with its company loaded, for the detail page.</summary>
        Task<Contact?> GetWithCompanyAsync(int id);

        Task<Contact?> GetAsync(int id);

        Task<ServiceResult<Contact>> CreateAsync(ContactInput input);

        Task<ServiceResult<Contact>> UpdateAsync(int id, ContactInput input);

        /// <summary>Deletes the contact and returns the name it had, for the confirmation message.</summary>
        Task<ServiceResult<string>> DeleteAsync(int id);
    }
}
