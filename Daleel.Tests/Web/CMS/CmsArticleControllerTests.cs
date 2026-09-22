using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.Controllers;
using Daleel.Models;
using Daleel.Tests.Common;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Tests.Web.CMS
{
    public class CmsArticleControllerTests
    {
        private readonly Mock<IArticleService> _mockArticles;
        private readonly Mock<IFileStorageService> _mockFiles;
        private readonly CmsArticleController _controller;

        public CmsArticleControllerTests()
        {
            _mockArticles = new Mock<IArticleService>();
            _mockFiles = new Mock<IFileStorageService>();
            _controller = new CmsArticleController(_mockArticles.Object, _mockFiles.Object).WithTestContext();
        }

        [Fact]
        public async Task Index_ReturnsViewResultWithPagedArticles()
        {
            // Arrange
            _mockArticles.Setup(a => a.SearchAsync(It.IsAny<ArticleQuery>()))
                .ReturnsAsync(new PagedResult<ArticleListItem>
                {
                    Items = new List<ArticleListItem> { new() { Id = 1, TitleEn = "Test Article" } },
                    TotalCount = 1
                });

            // Act
            var result = await _controller.Index(q: null, category: null, isPublished: null, page: 1);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<PagedResult<ArticleListItem>>().Subject;
            model.Items.Should().HaveCount(1);
        }

        [Fact]
        public void Create_Get_ReturnsViewResultWithNewModel()
        {
            // Act
            var result = _controller.Create();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.Model.Should().BeOfType<ArticleFormViewModel>();
        }

        [Fact]
        public async Task Create_Post_ValidModel_CreatesArticleAndRedirects()
        {
            // Arrange
            var model = new ArticleFormViewModel
            {
                TitleEn = "New Article",
                TitleAr = "مقال جديد",
                BodyHtmlEn = "<p>Content</p>",
                BodyHtmlAr = "<p>المحتوى</p>"
            };
            _mockArticles.Setup(a => a.CreateAsync(It.IsAny<ArticleInput>()))
                .ReturnsAsync(ServiceResult<ArticleDetailDto>.Success(new ArticleDetailDto { Id = 1, TitleEn = "New Article" }));

            // Act
            var result = await _controller.Create(model);

            // Assert
            var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirect.ActionName.Should().Be(nameof(CmsArticleController.Index));
            _controller.TempData["Success"].Should().NotBeNull();
        }

        [Fact]
        public async Task Create_Post_InvalidModelState_ReturnsViewWithModel()
        {
            // Arrange
            var model = new ArticleFormViewModel();
            _controller.ModelState.AddModelError("TitleEn", "Required");

            // Act
            var result = await _controller.Create(model);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.Model.Should().Be(model);
        }

        [Fact]
        public async Task TogglePublish_Post_TogglesAndRedirects()
        {
            // Arrange
            _mockArticles.Setup(a => a.TogglePublishAsync(1))
                .ReturnsAsync(ServiceResult<bool>.Success(true));

            // Act
            var result = await _controller.TogglePublish(1);

            // Assert
            var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirect.ActionName.Should().Be(nameof(CmsArticleController.Index));
        }

        [Fact]
        public async Task Delete_Post_DeletesAndRedirects()
        {
            // Arrange
            _mockArticles.Setup(a => a.DeleteAsync(1))
                .ReturnsAsync(ServiceResult.Success());

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirect.ActionName.Should().Be(nameof(CmsArticleController.Index));
        }
    }
}
