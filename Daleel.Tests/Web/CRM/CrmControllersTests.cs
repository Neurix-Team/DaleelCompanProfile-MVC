using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.Controllers;
using Daleel.DAL.Entities;
using Daleel.Models;
using Daleel.Tests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Tests.Web.CRM
{
    public class CrmControllersTests
    {
        [Fact]
        public async Task CrmAccountController_Login_ValidCredentials_RedirectsToDashboard()
        {
            // Arrange
            var mockAccountService = new Mock<IAccountService>();
            mockAccountService.Setup(a => a.SignInAsync("admin@daleel.sa", "Secret123!", false))
                .ReturnsAsync(SignInOutcome.Success);

            var controller = new CrmAccountController(mockAccountService.Object).WithTestContext();

            var loginModel = new LoginViewModel
            {
                Email = "admin@daleel.sa",
                Password = "Secret123!",
                RememberMe = false
            };

            // Act
            var result = await controller.Login(loginModel);

            // Assert
            var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirect.ActionName.Should().Be(nameof(CrmDashboardController.Index));
            redirect.ControllerName.Should().Be("CrmDashboard");
        }

        [Fact]
        public async Task CrmAccountController_Login_LockedOut_ReturnsViewWithLockoutError()
        {
            // Arrange
            var mockAccountService = new Mock<IAccountService>();
            mockAccountService.Setup(a => a.SignInAsync("locked@daleel.sa", "Secret123!", false))
                .ReturnsAsync(SignInOutcome.LockedOut);

            var controller = new CrmAccountController(mockAccountService.Object).WithTestContext();

            var loginModel = new LoginViewModel
            {
                Email = "locked@daleel.sa",
                Password = "Secret123!"
            };

            // Act
            var result = await controller.Login(loginModel);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.Model.Should().Be(loginModel);
            controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task CrmAccountController_Logout_SignsOutAndRedirectsToLogin()
        {
            // Arrange
            var mockAccountService = new Mock<IAccountService>();
            mockAccountService.Setup(a => a.SignOutAsync()).Returns(Task.CompletedTask);

            var controller = new CrmAccountController(mockAccountService.Object).WithTestContext();

            // Act
            var result = await controller.Logout();

            // Assert
            var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirect.ActionName.Should().Be(nameof(CrmAccountController.Login));
            mockAccountService.Verify(a => a.SignOutAsync(), Times.Once);
        }

        [Fact]
        public async Task CrmDashboardController_Index_ReturnsViewWithPopulatedKpis()
        {
            // Arrange
            var mockDashboard = new Mock<IDashboardService>();
            mockDashboard.Setup(d => d.GetKpisAsync())
                .ReturnsAsync(new DashboardKpis { TotalLeads = 15, TotalCompanies = 8 });
            mockDashboard.Setup(d => d.GetPipelineByCurrencyAsync())
                .ReturnsAsync(new List<CurrencyTotal> { new("SAR", 500000m, 5) });
            mockDashboard.Setup(d => d.GetUpcomingTasksAsync(5))
                .ReturnsAsync(new List<TaskListItem>());
            mockDashboard.Setup(d => d.GetRecentActivitiesAsync(6))
                .ReturnsAsync(new List<ActivityListItem>());

            var controller = new CrmDashboardController(mockDashboard.Object).WithTestContext();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<DashboardViewModel>().Subject;
            model.Kpis.TotalLeads.Should().Be(15);
            model.Pipeline.Should().HaveCount(1);
        }

        [Fact]
        public async Task CrmLeadController_Index_ReturnsPagedLeads()
        {
            // Arrange
            var mockLeads = new Mock<ILeadService>();
            var mockCompanies = new Mock<ICompanyService>();

            mockLeads.Setup(l => l.SearchAsync(It.IsAny<LeadQuery>()))
                .ReturnsAsync(new PagedResult<LeadListItem>
                {
                    Items = new List<LeadListItem> { new() { Id = 1, FirstName = "Prospect", LastName = "One" } },
                    TotalCount = 1
                });
            mockLeads.Setup(l => l.GetAssignableUsersAsync())
                .ReturnsAsync(new List<ListOption>());

            var controller = new CrmLeadController(mockLeads.Object, mockCompanies.Object).WithTestContext();

            // Act
            var result = await controller.Index(q: null, status: null, source: null, assignedToId: null);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<PagedResult<LeadListItem>>().Subject;
            model.Items.Should().HaveCount(1);
            model.Items[0].FullName.Should().Be("Prospect One");
        }

        [Fact]
        public async Task CrmDealController_Pipeline_ReturnsPipelineBoard()
        {
            // Arrange
            var mockDeals = new Mock<IDealService>();
            var mockCompanies = new Mock<ICompanyService>();
            var mockLeads = new Mock<ILeadService>();

            mockDeals.Setup(d => d.GetPipelineAsync(null))
                .ReturnsAsync(new PipelineBoard
                {
                    Columns = new List<PipelineColumn>
                    {
                        new(DealStage.New, new List<PipelineCard>())
                    }
                });
            mockLeads.Setup(l => l.GetAssignableUsersAsync())
                .ReturnsAsync(new List<ListOption>());

            var controller = new CrmDealController(mockDeals.Object, mockCompanies.Object, mockLeads.Object).WithTestContext();

            // Act
            var result = await controller.Pipeline(null);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<PipelineBoard>().Subject;
            model.Columns.Should().HaveCount(1);
        }

        [Fact]
        public async Task CrmActivityController_Create_LogsActivityAndRedirects()
        {
            // Arrange
            var mockActivities = new Mock<IActivityService>();
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            var mockUsers = new Mock<UserManager<ApplicationUser>>(userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            mockActivities.Setup(a => a.CreateAsync(It.IsAny<ActivityInput>(), It.IsAny<string?>()))
                .ReturnsAsync(ServiceResult<Activity>.Success(new Activity { Id = 1, Subject = "Phone Call" }));

            var controller = new CrmActivityController(mockActivities.Object, mockUsers.Object).WithTestContext();

            var formModel = new ActivityFormViewModel
            {
                EntityType = CrmEntityType.Company,
                EntityId = 5,
                Type = ActivityType.Call,
                Subject = "Discuss Renewal",
                ReturnUrl = "/crm/companies/5"
            };

            // Act
            var result = await controller.Create(formModel);

            // Assert
            var redirect = result.Should().BeOfType<RedirectResult>().Subject;
            redirect.Url.Should().Be("/crm/companies/5");
            controller.TempData["Success"].Should().NotBeNull();
        }
    }
}
