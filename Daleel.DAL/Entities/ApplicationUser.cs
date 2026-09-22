using Microsoft.AspNetCore.Identity;

namespace Daleel.DAL.Entities
{
    /// <summary>
    /// A CRM user (company employee). Extends the ASP.NET Identity user with the
    /// name fields the CRM needs when displaying record owners / assigned employees.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public string FirstName { get; set; } = string.Empty;

        [PersonalData]
        public string LastName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
