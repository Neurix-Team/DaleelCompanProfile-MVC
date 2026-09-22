using Daleel.BAL.Models;
using Daleel.DAL.Entities;
using Daleel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Daleel.Controllers
{
    [Route("cms/users")]
    [Authorize(Roles = "Admin")]
    public class CmsUserController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly RoleManager<IdentityRole> _roles;

        public CmsUserController(
            UserManager<ApplicationUser> users,
            RoleManager<IdentityRole> roles)
        {
            _users = users;
            _roles = roles;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var userEntities = await _users.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var viewModels = new List<CmsUserListItemViewModel>();

            foreach (var user in userEntities)
            {
                var roles = await _users.GetRolesAsync(user);
                viewModels.Add(new CmsUserListItemViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = roles,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                });
            }

            return View(viewModels);
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View(new CreateCmsUserViewModel());
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCmsUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _users.FindByEmailAsync(model.Email.Trim());
            if (existing != null)
            {
                ModelState.AddModelError(nameof(model.Email), "A user with this email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email.Trim(),
                Email = model.Email.Trim(),
                EmailConfirmed = true,
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _users.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var err in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                return View(model);
            }

            var role = model.Role == "Admin" ? CrmRoles.Admin : CrmRoles.Staff;
            if (!await _roles.RoleExistsAsync(role))
            {
                await _roles.CreateAsync(new IdentityRole(role));
            }
            await _users.AddToRoleAsync(user, role);

            TempData["Success"] = $"User \"{user.FullName}\" ({user.Email}) was created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _users.GetRolesAsync(user);
            var model = new EditCmsUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = roles.FirstOrDefault() ?? "Admin",
                IsActive = user.IsActive
            };

            return View(model);
        }

        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditCmsUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _users.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = model.FirstName.Trim();
            user.LastName = model.LastName.Trim();
            user.Email = model.Email.Trim();
            user.UserName = model.Email.Trim();
            user.IsActive = model.IsActive;

            var updateResult = await _users.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                return View(model);
            }

            var currentRoles = await _users.GetRolesAsync(user);
            await _users.RemoveFromRolesAsync(user, currentRoles);
            var targetRole = model.Role == "Admin" ? CrmRoles.Admin : CrmRoles.Staff;
            if (!await _roles.RoleExistsAsync(targetRole))
            {
                await _roles.CreateAsync(new IdentityRole(targetRole));
            }
            await _users.AddToRoleAsync(user, targetRole);

            TempData["Success"] = $"User \"{user.FullName}\" was updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("reset-password/{id}")]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ResetUserPasswordViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName
            };

            return View(model);
        }

        [HttpPost("reset-password/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, ResetUserPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _users.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var result = await _users.ResetPasswordAsync(user, token, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                return View(model);
            }

            TempData["Success"] = $"Password for \"{user.FullName}\" has been reset successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("toggle-active/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Prevent self-lockout
            if (user.UserName == User.Identity?.Name)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            await _users.UpdateAsync(user);

            var status = user.IsActive ? "activated" : "deactivated";
            TempData["Success"] = $"User \"{user.FullName}\" was {status}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
