using Daleel.BAL.Models;

namespace Daleel.BAL.Services.Interfaces
{
    /// <summary>
    /// Brings the CRM database up to date and seeds the roles and initial administrator.
    /// Owned by the business layer because "which roles exist" and "there must be an
    /// admin" are business rules, not schema concerns.
    /// </summary>
    public interface ICrmInitializer
    {
        Task InitializeAsync(CrmSeedOptions options);
    }
}
