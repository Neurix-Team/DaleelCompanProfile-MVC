using System.Security.Claims;
using Daleel.BAL.Models;

namespace Daleel.BAL.Services.Interfaces
{
    /// <summary>
    /// CRM sign-in. Wraps ASP.NET Identity so controllers depend on the business layer
    /// rather than on SignInManager directly.
    /// </summary>
    public interface IAccountService
    {
        Task<SignInOutcome> SignInAsync(string email, string password, bool rememberMe);

        Task SignOutAsync();

        bool IsSignedIn(ClaimsPrincipal user);
    }
}
