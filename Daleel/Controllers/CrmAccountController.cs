using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Controllers
{
    /// <summary>
    /// Sign-in / sign-out for the private CRM area. Identity is reached through
    /// <see cref="IAccountService"/>; this controller only decides what the user sees.
    /// </summary>
    [Route("crm/account")]
    [AllowAnonymous]
    public class CrmAccountController : Controller
    {
        private readonly IAccountService _accounts;

        public CrmAccountController(IAccountService accounts) => _accounts = accounts;

        [HttpGet("login")]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_accounts.IsSignedIn(User))
            {
                return RedirectToAction(nameof(CrmDashboardController.Index), "CrmDashboard");
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var outcome = await _accounts.SignInAsync(model.Email, model.Password, model.RememberMe);

            switch (outcome)
            {
                case SignInOutcome.Success:
                    return RedirectToLocalOrDashboard(model.ReturnUrl);

                case SignInOutcome.LockedOut:
                    ModelState.AddModelError(string.Empty,
                        "This account is temporarily locked due to multiple failed sign-in attempts. Please try again later.");
                    return View(model);

                default:
                    // Deliberately generic so we do not reveal whether the account exists.
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    return View(model);
            }
        }

        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _accounts.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet("access-denied")]
        public IActionResult AccessDenied() => View();

        private IActionResult RedirectToLocalOrDashboard(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(CrmDashboardController.Index), "CrmDashboard");
        }
    }
}
