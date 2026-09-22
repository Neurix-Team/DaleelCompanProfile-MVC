namespace Daleel.BAL.Models
{
    /// <summary>CRM role names. The single source of truth for authorization checks.</summary>
    public static class CrmRoles
    {
        public const string Admin = "Admin";

        public const string Staff = "Staff";

        public static IReadOnlyList<string> All { get; } = new[] { Admin, Staff };
    }

    /// <summary>
    /// Result of a sign-in attempt. Deliberately an outcome rather than a message —
    /// what the user is told about a failed login is a presentation decision, and a
    /// security-sensitive one.
    /// </summary>
    public enum SignInOutcome
    {
        Success,
        LockedOut,
        InvalidCredentials
    }

    /// <summary>Initial administrator to create on first run, supplied from configuration.</summary>
    public class CrmSeedOptions
    {
        public string AdminEmail { get; set; } = string.Empty;

        public string AdminPassword { get; set; } = string.Empty;

        public string AdminFirstName { get; set; } = "Daleel";

        public string AdminLastName { get; set; } = "Administrator";
    }
}
