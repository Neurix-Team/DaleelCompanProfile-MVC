using Daleel.BAL.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Daleel.Tests.BAL.CMS
{
    public class LocalFileStorageServiceTests : IDisposable
    {
        private readonly string _tempWebRoot;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly Mock<ILogger<LocalFileStorageService>> _mockLogger;
        private readonly LocalFileStorageService _service;

        public LocalFileStorageServiceTests()
        {
            _tempWebRoot = Path.Combine(Path.GetTempPath(), "DaleelTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempWebRoot);

            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockEnv.Setup(e => e.WebRootPath).Returns(_tempWebRoot);
            _mockEnv.Setup(e => e.ContentRootPath).Returns(_tempWebRoot);

            _mockLogger = new Mock<ILogger<LocalFileStorageService>>();
            _service = new LocalFileStorageService(_mockEnv.Object, _mockLogger.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempWebRoot))
            {
                try { Directory.Delete(_tempWebRoot, true); } catch { }
            }
        }

        [Fact]
        public async Task SaveFileAsync_AllowedImageExtension_SavesFileAndReturnsPath()
        {
            // Arrange
            var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // Dummy JPG header
            using var stream = new MemoryStream(content);

            // Act
            var path = await _service.SaveFileAsync(stream, "sample-image.jpg", "test-folder");

            // Assert
            path.Should().NotBeNull();
            path.Should().StartWith("/uploads/test-folder/");
            path.Should().EndWith(".jpg");

            var relativePart = path!.TrimStart('/');
            var fullPhysicalPath = Path.Combine(_tempWebRoot, relativePart);
            File.Exists(fullPhysicalPath).Should().BeTrue();
        }

        [Theory]
        [InlineData("malicious.exe")]
        [InlineData("script.sh")]
        [InlineData("bad.dll")]
        [InlineData("document.pdf")]
        public async Task SaveFileAsync_DisallowedExtension_ReturnsNull(string fileName)
        {
            // Arrange
            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

            // Act
            var path = await _service.SaveFileAsync(stream, fileName, "articles");

            // Assert
            path.Should().BeNull();
        }

        [Fact]
        public async Task SaveFileAsync_EmptyOrNullStream_ReturnsNull()
        {
            // Arrange
            using var emptyStream = new MemoryStream();

            // Act
            var pathNull = await _service.SaveFileAsync(null!, "test.jpg");
            var pathEmpty = await _service.SaveFileAsync(emptyStream, "test.jpg");

            // Assert
            pathNull.Should().BeNull();
            pathEmpty.Should().BeNull();
        }

        [Fact]
        public async Task DeleteFile_RemovesExistingFile()
        {
            // Arrange
            var content = new byte[] { 1, 2, 3, 4 };
            using var stream = new MemoryStream(content);
            var path = await _service.SaveFileAsync(stream, "to-delete.png", "temp");

            var relativePart = path!.TrimStart('/');
            var fullPhysicalPath = Path.Combine(_tempWebRoot, relativePart);
            File.Exists(fullPhysicalPath).Should().BeTrue();

            // Act
            _service.DeleteFile(path);

            // Assert
            File.Exists(fullPhysicalPath).Should().BeFalse();
        }
    }
}
