using System.Security.Claims;
using Daleel.BAL.Models;

namespace Daleel.Models
{
    public static class UserRoleDisplay
    {
        public static string GetResourceKey(ClaimsPrincipal user)
        {
            if (user.IsInRole(CrmRoles.Admin))
            {
                return "SharedRoleAdministrator";
            }

            if (user.IsInRole(CrmRoles.Staff))
            {
                return "SharedRoleStaff";
            }

            return "SharedRoleUser";
        }
    }
}
