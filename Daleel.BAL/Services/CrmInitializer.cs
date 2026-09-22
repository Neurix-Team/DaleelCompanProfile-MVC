using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Daleel.BAL.Services
{
    /// <inheritdoc />
    public class CrmInitializer : ICrmInitializer
    {
        private readonly ApplicationDbContext _db;
        private readonly RoleManager<IdentityRole> _roles;
        private readonly UserManager<ApplicationUser> _users;

        public CrmInitializer(
            ApplicationDbContext db,
            RoleManager<IdentityRole> roles,
            UserManager<ApplicationUser> users)
        {
            _db = db;
            _roles = roles;
            _users = users;
        }

        /// <summary>Idempotent — safe to run on every application start.</summary>
        public async Task InitializeAsync(CrmSeedOptions options)
        {
            await _db.Database.MigrateAsync();

            foreach (var role in CrmRoles.All)
            {
                if (!await _roles.RoleExistsAsync(role))
                {
                    await _roles.CreateAsync(new IdentityRole(role));
                }
            }

            if (string.IsNullOrWhiteSpace(options.AdminEmail) ||
                string.IsNullOrWhiteSpace(options.AdminPassword))
            {
                return;
            }

            var admin = await _users.FindByEmailAsync(options.AdminEmail);
            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    UserName = options.AdminEmail,
                    Email = options.AdminEmail,
                    EmailConfirmed = true,
                    FirstName = options.AdminFirstName,
                    LastName = options.AdminLastName
                };

                var result = await _users.CreateAsync(admin, options.AdminPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException(
                        $"Failed to create the seed administrator account: {errors}");
                }
            }

            if (!await _users.IsInRoleAsync(admin, CrmRoles.Admin))
            {
                await _users.AddToRoleAsync(admin, CrmRoles.Admin);
            }
        }
    }
}
