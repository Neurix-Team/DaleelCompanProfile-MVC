using Daleel.BAL.Models;
using Daleel.DAL.Entities;
using Microsoft.AspNetCore.Identity;

namespace Daleel.Services
{
    /// <summary>
    /// One-off CLI mode: creates a CRM/CMS login, or resets the password of an existing one.
    ///
    ///   dotnet Daleel.dll create-user someone@example.com [--role Admin|Staff] [--first Name] [--last Name]
    ///
    /// The password is never taken from the command line (it would land in shell history and
    /// `ps`); it is prompted for without echo, or read from stdin when input is piped.
    /// </summary>
    public static class CreateUserCommand
    {
        public const string Name = "create-user";

        public static bool IsInvocation(string[] args) =>
            args.Length > 0 && string.Equals(args[0], Name, StringComparison.OrdinalIgnoreCase);

        /// <param name="args">Arguments after the command name.</param>
        /// <param name="readSecret">Reads one password entry for the given prompt.</param>
        /// <returns>Process exit code: 0 on success.</returns>
        public static async Task<int> RunAsync(
            IServiceProvider services, string[] args, Func<string, string> readSecret, TextWriter output)
        {
            var email = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal))?.Trim();
            var role = Option(args, "--role") ?? CrmRoles.Admin;
            var firstName = Option(args, "--first") ?? string.Empty;
            var lastName = Option(args, "--last") ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                output.WriteLine($"Usage: dotnet Daleel.dll {Name} <email> [--role {string.Join("|", CrmRoles.All)}] [--first Name] [--last Name]");
                return 2;
            }

            role = CrmRoles.All.FirstOrDefault(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            if (role.Length == 0)
            {
                output.WriteLine($"Unknown role. Use one of: {string.Join(", ", CrmRoles.All)}.");
                return 2;
            }

            var password = readSecret("Password: ");
            if (string.IsNullOrEmpty(password))
            {
                output.WriteLine("Password is required.");
                return 2;
            }

            if (readSecret("Confirm password: ") != password)
            {
                output.WriteLine("Passwords do not match.");
                return 2;
            }

            var users = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roles = services.GetRequiredService<RoleManager<IdentityRole>>();

            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole(role));
            }

            var user = await users.FindByEmailAsync(email);
            var created = user == null;

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = firstName,
                    LastName = lastName,
                    IsActive = true
                };

                if (!Report(await users.CreateAsync(user, password), output))
                {
                    return 1;
                }
            }
            else
            {
                // Validate first so a weak password never leaves the account without one.
                foreach (var validator in users.PasswordValidators)
                {
                    if (!Report(await validator.ValidateAsync(users, user, password), output))
                    {
                        return 1;
                    }
                }

                var token = await users.GeneratePasswordResetTokenAsync(user);
                if (!Report(await users.ResetPasswordAsync(user, token, password), output))
                {
                    return 1;
                }

                user.IsActive = true;
                if (firstName.Length > 0) user.FirstName = firstName;
                if (lastName.Length > 0) user.LastName = lastName;
                await users.SetLockoutEndDateAsync(user, null);
                await users.ResetAccessFailedCountAsync(user);
                Report(await users.UpdateAsync(user), output);
            }

            if (!await users.IsInRoleAsync(user, role) && !Report(await users.AddToRoleAsync(user, role), output))
            {
                return 1;
            }

            output.WriteLine(created
                ? $"Created {email} with role {role}."
                : $"Updated {email}: password reset, account active, role {role}.");
            return 0;
        }

        /// <summary>Reads a password without echoing it, or a plain line when stdin is piped.</summary>
        public static string ReadConsoleSecret(string prompt)
        {
            if (Console.IsInputRedirected)
            {
                return Console.ReadLine() ?? string.Empty;
            }

            Console.Write(prompt);
            var buffer = new System.Text.StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (buffer.Length > 0) buffer.Length--;
                    continue;
                }

                if (!char.IsControl(key.KeyChar))
                {
                    buffer.Append(key.KeyChar);
                }
            }

            Console.WriteLine();
            return buffer.ToString();
        }

        private static string? Option(string[] args, string name)
        {
            var index = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
            return index >= 0 && index + 1 < args.Length ? args[index + 1].Trim() : null;
        }

        private static bool Report(IdentityResult result, TextWriter output)
        {
            foreach (var error in result.Errors)
            {
                output.WriteLine($"Error: {error.Description}");
            }

            return result.Succeeded;
        }
    }
}
