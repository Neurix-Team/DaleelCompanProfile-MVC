using System.Security.Claims;
using Daleel.BAL.Models;
using Daleel.Models;

namespace Daleel.Tests.Web.Shared
{
    public class UserRoleDisplayTests
    {
        [Theory]
        [InlineData(CrmRoles.Admin, "SharedRoleAdministrator")]
        [InlineData(CrmRoles.Staff, "SharedRoleStaff")]
        [InlineData(null, "SharedRoleUser")]
        public void GetResourceKey_UsesTheAuthenticatedRole(string? role, string expectedKey)
        {
            var claims = role is null
                ? Array.Empty<Claim>()
                : new[] { new Claim(ClaimTypes.Role, role) };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

            UserRoleDisplay.GetResourceKey(user).Should().Be(expectedKey);
        }

        [Fact]
        public void GetResourceKey_PrefersAdministratorForMultiRoleUsers()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.Role, CrmRoles.Staff),
                    new Claim(ClaimTypes.Role, CrmRoles.Admin)
                },
                "Test"));

            UserRoleDisplay.GetResourceKey(user).Should().Be("SharedRoleAdministrator");
        }
    }
}
