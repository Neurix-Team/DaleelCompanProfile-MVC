using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.Controllers;
using Daleel.Models;
using Daleel.Tests.Common;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Tests.Web
{
    public class HomeControllerTests
    {
        private readonly Mock<ILeadService> _mockLeads;
        private readonly Mock<IArticleService> _mockArticles;
        private readonly Mock<IPageSectionService> _mockSections;
        private readonly Mock<IBrandProfileService> _mockBrands;
        private readonly Mock<ICmsServiceService> _mockServices;
        private readonly Mock<ICmsProjectService> _mockProjects;
        private readonly Mock<ITeamMemberService> _mockTeam;
        private readonly Mock<ITestimonialService> _mockTestimonials;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            _mockLeads = new Mock<ILeadService>();
            _mockArticles = new Mock<IArticleService>();
            _mockSections = new Mock<IPageSectionService>();
            _mockBrands = new Mock<IBrandProfileService>();
            _mockServices = new Mock<ICmsServiceService>();
            _mockProjects = new Mock<ICmsProjectService>();
            _mockTeam = new Mock<ITeamMemberService>();
            _mockTestimonials = new Mock<ITestimonialService>();

            _controller = new HomeController(
                _mockLeads.Object,
                _mockArticles.Object,
                _mockSections.Object,
                _mockBrands.Object,
                _mockServices.Object,
                _mockProjects.Object,
                _mockTeam.Object,
                _mockTestimonials.Object)
                .WithTestContext();
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithPopulatedHomePageViewModel()
        {
            // Arrange
            _mockSections.Setup(s => s.GetPageContentMapAsync("home", It.IsAny<string>()))
                .ReturnsAsync(new Dictionary<string, string> { { "hero_title", "Welcome" } });
            _mockBrands.Setup(b => b.GetBySlugAsync("daleel", false))
                .ReturnsAsync(new BrandProfileDetailDto { NameEn = "Daleel" });
            _mockServices.Setup(s => s.GetByBrandSlugAsync("daleel", true))
                .ReturnsAsync(new List<CmsServiceListItemDto>());
            _mockProjects.Setup(p => p.GetByBrandSlugAsync("daleel", true))
                .ReturnsAsync(new List<CmsProjectListItemDto>());
            _mockTeam.Setup(t => t.GetByBrandSlugAsync("daleel", true))
                .ReturnsAsync(new List<TeamMemberListItemDto>());
            _mockTestimonials.Setup(t => t.GetByBrandSlugAsync("daleel", true))
                .ReturnsAsync(new List<TestimonialListItemDto>());
            _mockArticles.Setup(a => a.GetRecentPublishedAsync(3))
                .ReturnsAsync(new List<ArticleListItem>());

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<HomePageViewModel>().Subject;
            model.Brand.Should().NotBeNull();
            model.Brand!.NameEn.Should().Be("Daleel");

            // Page sections now travel on ViewData for every public page, so one helper
            // (ViewData.PageContent()) serves them all instead of a per-model property.
            var content = viewResult.ViewData["PageContent"]
                .Should().BeAssignableTo<IReadOnlyDictionary<string, string>>().Subject;
            content.Should().ContainKey("hero_title");
        }

        [Fact]
        public async Task Article_WhenArticleFound_ReturnsViewResultWithArticle()
        {
            // Arrange
            var article = new ArticleDetailDto
            {
                Id = 1,
                Slug = "growth-guide",
                TitleEn = "Growth Guide",
                IsPublished = true
            };
            _mockArticles.Setup(a => a.GetBySlugAsync("growth-guide", true))
                .ReturnsAsync(article);
            _mockArticles.Setup(a => a.GetRecentPublishedAsync(3))
                .ReturnsAsync(new List<ArticleListItem>());

            // Act
            var result = await _controller.Article("growth-guide");

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<ArticleDetailDto>().Subject;
            model.Slug.Should().Be("growth-guide");
        }

        [Fact]
        public async Task Article_WhenArticleNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockArticles.Setup(a => a.GetBySlugAsync("non-existent", true))
                .ReturnsAsync((ArticleDetailDto?)null);

            // Act
            var result = await _controller.Article("non-existent");

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Blog_ReturnsViewResultWithArticlesAndFilters()
        {
            // Arrange
            _mockArticles.Setup(a => a.SearchAsync(It.IsAny<ArticleQuery>()))
                .ReturnsAsync(new PagedResult<ArticleListItem>
                {
                    Items = new List<ArticleListItem> { new() { TitleEn = "Blog Post 1" } },
                    TotalCount = 1
                });
            _mockArticles.Setup(a => a.GetDistinctCategoriesAsync())
                .ReturnsAsync(new List<string> { "Strategy", "Tech" });
            _mockArticles.Setup(a => a.GetDistinctTagsAsync())
                .ReturnsAsync(new List<string> { "AI", "Cloud" });

            // Act
            var result = await _controller.Blog(q: "AI", category: "Tech", tag: "Cloud", page: 1);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<BlogIndexViewModel>().Subject;
            model.Articles.Items.Should().HaveCount(1);
            model.SearchTerm.Should().Be("AI");
            model.SelectedCategory.Should().Be("Tech");
            model.SelectedTag.Should().Be("Cloud");
            model.Categories.Should().Contain("Tech");
            model.Tags.Should().Contain("Cloud");
        }

        [Fact]
        public async Task Contact_Post_ValidInput_CapturesLeadAndRedirects()
        {
            // Arrange
            var pageModel = new ContactPageViewModel
            {
                Form = new ContactInquiryViewModel
                {
                    FirstName = "Nasser",
                    LastName = "Al-Subaie",
                    Email = "nasser@example.com",
                    Subject = "General Inquiry",
                    Message = "Interested in AI platforms."
                }
            };
            _mockLeads.Setup(l => l.CaptureAsync(It.IsAny<LeadCaptureInput>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Contact(pageModel);

            // Assert
            var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirect.ActionName.Should().Be(nameof(HomeController.Contact));
            _controller.TempData["ContactSuccess"].Should().Be(true);
        }
    }
}
