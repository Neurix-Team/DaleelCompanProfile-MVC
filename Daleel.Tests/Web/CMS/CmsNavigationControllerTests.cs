using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.Controllers;
using Daleel.DAL.Entities;
using Daleel.Models;
using Daleel.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Daleel.Tests.Web.CMS
{
    public class CmsNavigationControllerTests
    {
        private readonly Mock<INavigationService> _mockNavService;
        private readonly CmsNavigationController _controller;

        public CmsNavigationControllerTests()
        {
            _mockNavService = new Mock<INavigationService>();
            _controller = new CmsNavigationController(_mockNavService.Object).WithTestContext();
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithNavigationIndexViewModel()
        {
            // Arrange
            _mockNavService.Setup(s => s.GetGroupedNavigationAsync())
                .ReturnsAsync(new GroupedNavigationDto
                {
                    HeaderLinks = new List<NavigationLinkDto>
                    {
                        new() { Id = 1, Location = NavigationLocation.Header, LabelEn = "Home", LabelAr = "الرئيسية", Url = "/", SortOrder = 1, IsActive = true }
                    }
                });

            // Act
            var result = await _controller.Index("header");

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<NavigationIndexViewModel>().Subject;
            model.ActiveTab.Should().Be("header");
            model.Navigation.HeaderLinks.Should().HaveCount(1);
        }

        [Fact]
        public void Create_Get_ReturnsViewResult_WithDefaultModel()
        {
            // Act
            var result = _controller.Create(NavigationLocation.Footer, FooterSection.Platform);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<NavigationFormViewModel>().Subject;
            model.Location.Should().Be(NavigationLocation.Footer);
            model.Section.Should().Be(FooterSection.Platform);
            model.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Create_Post_ValidModel_RedirectsToIndexWithProperTab()
        {
            // Arrange
            var form = new NavigationFormViewModel
            {
                Location = NavigationLocation.Header,
                LabelEn = "Services",
                LabelAr = "خدمات",
                Url = "/services",
                SortOrder = 10,
                IsActive = true
            };

            _mockNavService.Setup(s => s.CreateAsync(It.IsAny<NavigationLinkInput>()))
                .ReturnsAsync(ServiceResult<NavigationLinkDto>.Success(new NavigationLinkDto { Id = 100 }));

            // Act
            var result = await _controller.Create(form);

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be(nameof(CmsNavigationController.Index));
            redirectResult.RouteValues.Should().ContainKey("tab").WhoseValue.Should().Be("header");
        }

        [Fact]
        public async Task Create_Post_FooterWithoutSection_ReturnsViewWithModelError()
        {
            // Arrange
            var form = new NavigationFormViewModel
            {
                Location = NavigationLocation.Footer,
                Section = null,
                LabelEn = "Support",
                LabelAr = "الدعم",
                Url = "/support"
            };

            // Act
            var result = await _controller.Create(form);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            _controller.ModelState.IsValid.Should().BeFalse();
            _controller.ModelState.ContainsKey("Section").Should().BeTrue();
        }

        [Fact]
        public async Task Edit_Get_NonExistent_ReturnsNotFound()
        {
            // Arrange
            _mockNavService.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((NavigationLinkDto?)null);

            // Act
            var result = await _controller.Edit(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Edit_Get_Existing_ReturnsViewResult()
        {
            // Arrange
            _mockNavService.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(new NavigationLinkDto
                {
                    Id = 1,
                    Location = NavigationLocation.Header,
                    LabelEn = "Home",
                    LabelAr = "الرئيسية",
                    Url = "/",
                    SortOrder = 1,
                    IsActive = true
                });

            // Act
            var result = await _controller.Edit(1);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<NavigationFormViewModel>().Subject;
            model.Id.Should().Be(1);
            model.LabelEn.Should().Be("Home");
        }

        [Fact]
        public async Task Edit_Post_ValidUpdate_RedirectsToIndex()
        {
            // Arrange
            var form = new NavigationFormViewModel
            {
                Id = 1,
                Location = NavigationLocation.Header,
                LabelEn = "Homepage",
                LabelAr = "الصفحة الرئيسية",
                Url = "/",
                SortOrder = 1,
                IsActive = true
            };

            _mockNavService.Setup(s => s.UpdateAsync(1, It.IsAny<NavigationLinkInput>()))
                .ReturnsAsync(ServiceResult<NavigationLinkDto>.Success(new NavigationLinkDto { Id = 1 }));

            // Act
            var result = await _controller.Edit(1, form);

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be(nameof(CmsNavigationController.Index));
        }

        [Fact]
        public async Task ToggleActive_ExistingId_RedirectsToIndexWithTab()
        {
            // Arrange
            _mockNavService.Setup(s => s.ToggleActiveAsync(1))
                .ReturnsAsync(ServiceResult<bool>.Success(false));

            // Act
            var result = await _controller.ToggleActive(1, "platform");

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be(nameof(CmsNavigationController.Index));
            redirectResult.RouteValues!["tab"].Should().Be("platform");
        }

        [Fact]
        public async Task Delete_ExistingId_RedirectsToIndexWithTab()
        {
            // Arrange
            _mockNavService.Setup(s => s.DeleteAsync(1))
                .ReturnsAsync(ServiceResult<bool>.Success(true));

            // Act
            var result = await _controller.Delete(1, "company");

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be(nameof(CmsNavigationController.Index));
            redirectResult.RouteValues!["tab"].Should().Be("company");
        }
    }
}
