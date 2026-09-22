using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.Controllers;
using Daleel.Models;
using Daleel.Tests.Common;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Tests.Web.CMS
{
    public class CmsControllersTests
    {
        [Fact]
        public async Task CmsDashboardController_Index_ReturnsStatsView()
        {
            // Arrange
            var mockArticles = new Mock<IArticleService>();
            mockArticles.Setup(a => a.GetDashboardStatsAsync())
                .ReturnsAsync(new CmsDashboardStats { TotalArticles = 10, PublishedArticles = 8 });

            var controller = new CmsDashboardController(mockArticles.Object).WithTestContext();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<CmsDashboardStats>().Subject;
            model.TotalArticles.Should().Be(10);
        }

        [Fact]
        public async Task CmsBrandController_Index_ReturnsAllBrands()
        {
            // Arrange
            var mockBrands = new Mock<IBrandProfileService>();
            var mockFiles = new Mock<IFileStorageService>();
            mockBrands.Setup(b => b.GetAllAsync(false))
                .ReturnsAsync(new List<BrandProfileListItemDto>
                {
                    new() { Id = 1, NameEn = "Daleel", Slug = "daleel" }
                });

            var controller = new CmsBrandController(mockBrands.Object, mockFiles.Object).WithTestContext();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeAssignableTo<IEnumerable<BrandProfileListItemDto>>().Subject;
            model.Should().HaveCount(1);
        }

        [Fact]
        public async Task CmsServiceController_Index_ReturnsPagedServices()
        {
            // Arrange
            var mockServices = new Mock<ICmsServiceService>();
            var mockBrands = new Mock<IBrandProfileService>();
            var mockFiles = new Mock<IFileStorageService>();

            mockServices.Setup(s => s.SearchAsync(It.IsAny<CmsServiceQuery>()))
                .ReturnsAsync(new PagedResult<CmsServiceListItemDto>
                {
                    Items = new List<CmsServiceListItemDto> { new() { Id = 1, NameEn = "Service 1" } },
                    TotalCount = 1
                });
            mockBrands.Setup(b => b.GetAllAsync(false))
                .ReturnsAsync(new List<BrandProfileListItemDto>());

            var controller = new CmsServiceController(mockServices.Object, mockBrands.Object, mockFiles.Object).WithTestContext();

            // Act
            var result = await controller.Index(new CmsServiceQuery());

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<PagedResult<CmsServiceListItemDto>>().Subject;
            model.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task CmsProjectController_Index_ReturnsPagedProjects()
        {
            // Arrange
            var mockProjects = new Mock<ICmsProjectService>();
            var mockBrands = new Mock<IBrandProfileService>();
            var mockFiles = new Mock<IFileStorageService>();

            mockProjects.Setup(p => p.SearchAsync(It.IsAny<CmsProjectQuery>()))
                .ReturnsAsync(new PagedResult<CmsProjectListItemDto>
                {
                    Items = new List<CmsProjectListItemDto> { new() { Id = 1, NameEn = "Project 1" } },
                    TotalCount = 1
                });
            mockBrands.Setup(b => b.GetAllAsync(false))
                .ReturnsAsync(new List<BrandProfileListItemDto>());

            var controller = new CmsProjectController(mockProjects.Object, mockBrands.Object, mockFiles.Object).WithTestContext();

            // Act
            var result = await controller.Index(new CmsProjectQuery());

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<PagedResult<CmsProjectListItemDto>>().Subject;
            model.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task TeamMemberController_Index_ReturnsPagedTeamMembers()
        {
            // Arrange
            var mockTeam = new Mock<ITeamMemberService>();
            var mockBrands = new Mock<IBrandProfileService>();
            var mockFiles = new Mock<IFileStorageService>();

            mockTeam.Setup(t => t.SearchAsync(It.IsAny<TeamMemberQuery>()))
                .ReturnsAsync(new PagedResult<TeamMemberListItemDto>
                {
                    Items = new List<TeamMemberListItemDto> { new() { Id = 1, NameEn = "Member 1" } },
                    TotalCount = 1
                });
            mockBrands.Setup(b => b.GetAllAsync(false))
                .ReturnsAsync(new List<BrandProfileListItemDto>());

            var controller = new TeamMemberController(mockTeam.Object, mockBrands.Object, mockFiles.Object).WithTestContext();

            // Act
            var result = await controller.Index(new TeamMemberQuery());

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<PagedResult<TeamMemberListItemDto>>().Subject;
            model.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task TestimonialController_Index_ReturnsPagedTestimonials()
        {
            // Arrange
            var mockTestimonials = new Mock<ITestimonialService>();
            var mockBrands = new Mock<IBrandProfileService>();
            var mockFiles = new Mock<IFileStorageService>();

            mockTestimonials.Setup(t => t.SearchAsync(It.IsAny<TestimonialQuery>()))
                .ReturnsAsync(new PagedResult<TestimonialListItemDto>
                {
                    Items = new List<TestimonialListItemDto> { new() { Id = 1, CustomerNameEn = "Client 1" } },
                    TotalCount = 1
                });
            mockBrands.Setup(b => b.GetAllAsync(false))
                .ReturnsAsync(new List<BrandProfileListItemDto>());

            var controller = new TestimonialController(mockTestimonials.Object, mockBrands.Object, mockFiles.Object).WithTestContext();

            // Act
            var result = await controller.Index(new TestimonialQuery());

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<PagedResult<TestimonialListItemDto>>().Subject;
            model.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task CmsPageController_Index_ReturnsAllPageKeys()
        {
            // Arrange
            var mockSections = new Mock<IPageSectionService>();
            mockSections.Setup(s => s.GetDistinctPagesAsync())
                .ReturnsAsync(new List<string> { "home", "about", "trust" });

            var controller = new CmsPageController(mockSections.Object).WithTestContext();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeAssignableTo<IEnumerable<string>>().Subject;
            model.Should().Equal("home", "about", "trust");
        }

        [Fact]
        public async Task CmsPageController_Edit_SyncsMissingSectionsAndReturnsModel()
        {
            // Arrange
            var mockSections = new Mock<IPageSectionService>();
            mockSections.Setup(s => s.SyncMissingSectionKeysAsync())
                .ReturnsAsync(5);
            mockSections.Setup(s => s.GetSectionsByPageAsync("home"))
                .ReturnsAsync(new List<PageSectionDto>
                {
                    new PageSectionDto { PageKey = "home", SectionKey = "hero_title", ValueEn = "Title", ValueAr = "عنوان", DataType = "Text" },
                    new PageSectionDto { PageKey = "home", SectionKey = "services_badge", ValueEn = "Solutions", ValueAr = "حلول", DataType = "Text" }
                });

            var controller = new CmsPageController(mockSections.Object).WithTestContext();

            // Act
            var result = await controller.Edit("home");

            // Assert
            mockSections.Verify(s => s.SyncMissingSectionKeysAsync(), Times.Once);
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<PageSectionsEditViewModel>().Subject;
            model.PageKey.Should().Be("home");
            model.Sections.Should().HaveCount(2);
            model.Sections[0].SectionKey.Should().Be("hero_title");
            model.Sections[1].SectionKey.Should().Be("services_badge");
        }
    }
}
