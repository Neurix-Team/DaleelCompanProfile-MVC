using System.Security.Claims;
using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Daleel.BAL.Services
{
    /// <inheritdoc />
    public class AccountService : IAccountService
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountService> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<SignInOutcome> SignInAsync(string email, string password, bool rememberMe)
        {
            // lockoutOnFailure keeps the brute-force protection configured in Program.cs active.
            var result = await _signInManager.PasswordSignInAsync(
                email, password, rememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("CRM user {Email} signed in.", email);
                return SignInOutcome.Success;
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("CRM account {Email} is locked out.", email);
                return SignInOutcome.LockedOut;
            }

            return SignInOutcome.InvalidCredentials;
        }

        public Task SignOutAsync() => _signInManager.SignOutAsync();

        public bool IsSignedIn(ClaimsPrincipal user) => _signInManager.IsSignedIn(user);
    }
}
