using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.Controllers;
using Daleel.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Daleel.Tests.Web.CMS
{
    public class CmsMediaControllerTests
    {
        private readonly Mock<IMediaLibraryService> _mockMedia;
        private readonly CmsMediaController _controller;
        private readonly TempDataDictionary _tempData;

        public CmsMediaControllerTests()
        {
            _mockMedia = new Mock<IMediaLibraryService>();
            _controller = new CmsMediaController(_mockMedia.Object);

            var httpContext = new DefaultHttpContext();
            _tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            _controller.TempData = _tempData;
        }

        [Fact]
        public async Task Index_ReturnsViewWithFilesAndFolders()
        {
            // Arrange
            _mockMedia.Setup(m => m.GetFilesAsync(It.IsAny<MediaLibraryQuery>()))
                .ReturnsAsync(new List<MediaFileDto>
                {
                    new() { FileName = "test.png", RelativeUrl = "/uploads/test.png", Folder = "general" }
                });
            _mockMedia.Setup(m => m.GetFoldersAsync())
                .ReturnsAsync(new List<string> { "general", "brands" });

            // Act
            var result = await _controller.Index(folder: null, q: null);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<MediaLibraryViewModel>().Subject;
            model.Files.Should().HaveCount(1);
            model.Folders.Should().HaveCount(2);
        }

        [Fact]
        public async Task CheckUsage_EmptyPath_ReturnsZeroCount()
        {
            // Act
            var result = await _controller.CheckUsage(string.Empty);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            jsonResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task CheckUsage_InUseFile_ReturnsUsageData()
        {
            // Arrange
            var usages = new List<MediaUsageDto>
            {
                new() { EntityType = "Brand", EntityName = "Daleel", PropertyName = "Logo" },
                new() { EntityType = "Service", EntityName = "AI Engine", PropertyName = "Service Image" }
            };
            _mockMedia.Setup(m => m.GetFileUsagesAsync("/uploads/logo.png"))
                .ReturnsAsync(usages);

            // Act
            var result = await _controller.CheckUsage("/uploads/logo.png");

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            jsonResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task Delete_InUseFile_WithoutForce_RedirectsWithWarning()
        {
            // Arrange
            var usages = new List<MediaUsageDto>
            {
                new() { EntityType = "Brand", EntityName = "Daleel", PropertyName = "Logo" }
            };
            _mockMedia.Setup(m => m.GetFileUsagesAsync("/uploads/logo.png"))
                .ReturnsAsync(usages);

            // Act
            var result = await _controller.Delete("/uploads/logo.png", folder: "brands", force: false);

            // Assert
            var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirect.ActionName.Should().Be(nameof(CmsMediaController.Index));
            _tempData.ContainsKey("Warning").Should().BeTrue();
            _mockMedia.Verify(m => m.DeleteFileAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Delete_InUseFile_WithForceTrue_ProceedsWithDeletion()
        {
            // Arrange
            _mockMedia.Setup(m => m.DeleteFileAsync("/uploads/logo.png"))
                .ReturnsAsync(ServiceResult.Success());

            // Act
            var result = await _controller.Delete("/uploads/logo.png", folder: "brands", force: true);

            // Assert
            var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirect.ActionName.Should().Be(nameof(CmsMediaController.Index));
            _tempData.ContainsKey("Success").Should().BeTrue();
            _mockMedia.Verify(m => m.DeleteFileAsync("/uploads/logo.png"), Times.Once);
        }

        [Fact]
        public async Task Delete_UnusedFile_DeletesSuccessfully()
        {
            // Arrange
            _mockMedia.Setup(m => m.GetFileUsagesAsync("/uploads/unused.png"))
                .ReturnsAsync(new List<MediaUsageDto>());
            _mockMedia.Setup(m => m.DeleteFileAsync("/uploads/unused.png"))
                .ReturnsAsync(ServiceResult.Success());

            // Act
            var result = await _controller.Delete("/uploads/unused.png", folder: "general", force: false);

            // Assert
            var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirect.ActionName.Should().Be(nameof(CmsMediaController.Index));
            _tempData.ContainsKey("Success").Should().BeTrue();
            _mockMedia.Verify(m => m.DeleteFileAsync("/uploads/unused.png"), Times.Once);
        }

        [Fact]
        public async Task UploadApi_ValidFile_ReturnsJsonSuccess()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var content = "dummy image content";
            var fileName = "hero-bg.png";
            var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(ms.Length);

            _mockMedia.Setup(m => m.UploadFileAsync(It.IsAny<Stream>(), fileName, "pages"))
                .ReturnsAsync(ServiceResult<string>.Success("/uploads/pages/hero-bg.png"));

            // Act
            var result = await _controller.UploadApi(fileMock.Object, "pages");

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            jsonResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task UploadApi_NullFile_ReturnsJsonError()
        {
            // Act
            var result = await _controller.UploadApi(null, "pages");

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            jsonResult.Value.Should().NotBeNull();
        }
    }
}
