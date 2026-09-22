using System.Reflection;
using Daleel.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Tests.Security
{
    public class ControllerAuthorizationTests
    {
        [Theory]
        [InlineData(typeof(CmsArticleController))]
        [InlineData(typeof(CmsBrandController))]
        [InlineData(typeof(CmsServiceController))]
        [InlineData(typeof(CmsProjectController))]
        [InlineData(typeof(TeamMemberController))]
        [InlineData(typeof(TestimonialController))]
        [InlineData(typeof(CmsPageController))]
        [InlineData(typeof(CmsDashboardController))]
        public void CmsControllers_MustHaveAdminAuthorizeAttribute(Type controllerType)
        {
            // Assert
            var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
            authorizeAttr.Should().NotBeNull($"Controller {controllerType.Name} must be protected with [Authorize]");
            authorizeAttr!.Roles.Should().Be("Admin", $"Controller {controllerType.Name} must restrict access to Admin role");
        }

        [Theory]
        [InlineData(typeof(CrmCompanyController))]
        [InlineData(typeof(CrmContactController))]
        [InlineData(typeof(CrmLeadController))]
        [InlineData(typeof(CrmDealController))]
        [InlineData(typeof(CrmActivityController))]
        [InlineData(typeof(CrmTaskController))]
        [InlineData(typeof(CrmDashboardController))]
        public void CrmControllers_MustHaveAuthorizeAttribute(Type controllerType)
        {
            // Assert
            var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
            authorizeAttr.Should().NotBeNull($"Controller {controllerType.Name} must be protected with [Authorize]");
        }

        [Fact]
        public void CrmAccountController_MustAllowAnonymous()
        {
            // Assert
            var allowAnonAttr = typeof(CrmAccountController).GetCustomAttribute<AllowAnonymousAttribute>();
            allowAnonAttr.Should().NotBeNull("CrmAccountController must have [AllowAnonymous] so unauthenticated users can access login");
        }

        [Theory]
        [InlineData(typeof(CmsArticleController))]
        [InlineData(typeof(CmsBrandController))]
        [InlineData(typeof(CmsServiceController))]
        [InlineData(typeof(CmsProjectController))]
        [InlineData(typeof(TeamMemberController))]
        [InlineData(typeof(TestimonialController))]
        [InlineData(typeof(CrmAccountController))]
        [InlineData(typeof(CrmLeadController))]
        [InlineData(typeof(CrmDealController))]
        [InlineData(typeof(CrmActivityController))]
        public void MutatingPostActions_MustHaveValidateAntiForgeryToken(Type controllerType)
        {
            // Arrange
            var postActions = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null)
                .ToList();

            // Assert
            foreach (var action in postActions)
            {
                var hasAntiForgeryToken = action.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() != null
                                         || controllerType.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() != null
                                         || action.GetCustomAttribute<AutoValidateAntiforgeryTokenAttribute>() != null
                                         || controllerType.GetCustomAttribute<AutoValidateAntiforgeryTokenAttribute>() != null;

                hasAntiForgeryToken.Should().BeTrue($"Action {controllerType.Name}.{action.Name} [HttpPost] must enforce AntiForgery validation");
            }
        }
    }
}
