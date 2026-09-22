using Daleel.BAL.Models;
using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Daleel.Tests.BAL.CMS
{
    public class MediaLibraryServiceTests : IDisposable
    {
        private readonly string _tempWebRoot;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly Mock<ILogger<LocalFileStorageService>> _mockStorageLogger;
        private readonly Mock<ILogger<MediaLibraryService>> _mockMediaLogger;
        private readonly ApplicationDbContext _db;
        private readonly LocalFileStorageService _storageService;
        private readonly MediaLibraryService _mediaService;

        public MediaLibraryServiceTests()
        {
            _tempWebRoot = Path.Combine(Path.GetTempPath(), "MediaLibTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempWebRoot);

            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockEnv.Setup(e => e.WebRootPath).Returns(_tempWebRoot);
            _mockEnv.Setup(e => e.ContentRootPath).Returns(_tempWebRoot);

            _mockStorageLogger = new Mock<ILogger<LocalFileStorageService>>();
            _mockMediaLogger = new Mock<ILogger<MediaLibraryService>>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);

            _storageService = new LocalFileStorageService(_mockEnv.Object, _mockStorageLogger.Object);
            _mediaService = new MediaLibraryService(_mockEnv.Object, _storageService, _db, _mockMediaLogger.Object);
        }

        public void Dispose()
        {
            _db.Dispose();
            if (Directory.Exists(_tempWebRoot))
            {
                try { Directory.Delete(_tempWebRoot, true); } catch { }
            }
        }

        [Fact]
        public async Task GetFilesAsync_ReturnsUploadedFiles()
        {
            // Arrange
            using var stream1 = new MemoryStream(new byte[] { 1, 2, 3 });
            using var stream2 = new MemoryStream(new byte[] { 4, 5, 6, 7 });

            await _storageService.SaveFileAsync(stream1, "logo.png", "brands");
            await _storageService.SaveFileAsync(stream2, "cover.jpg", "articles");

            // Act
            var allFiles = await _mediaService.GetFilesAsync(new MediaLibraryQuery());
            var brandFiles = await _mediaService.GetFilesAsync(new MediaLibraryQuery { Folder = "brands" });

            // Assert
            allFiles.Count.Should().Be(2);
            brandFiles.Count.Should().Be(1);
            brandFiles[0].Folder.Should().Be("brands");
            brandFiles[0].IsImage.Should().BeTrue();
        }

        [Fact]
        public async Task GetFoldersAsync_ReturnsKnownFolders()
        {
            // Act
            var folders = await _mediaService.GetFoldersAsync();

            // Assert
            folders.Should().Contain("articles");
            folders.Should().Contain("brands");
            folders.Should().Contain("services");
        }

        [Fact]
        public async Task DeleteFileAsync_SecurityTraversalPrevention_RejectsOutsideUploads()
        {
            // Act
            var result = await _mediaService.DeleteFileAsync("/etc/passwd");

            // Assert
            result.Succeeded.Should().BeFalse();
            result.FirstErrorMessage.Should().Contain("Cannot delete files outside of uploads directory");
        }

        [Fact]
        public async Task DeleteFileAsync_ValidUploadPath_DeletesFile()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[] { 10, 20, 30 });
            var savedPath = await _storageService.SaveFileAsync(stream, "temp.png", "general");
            savedPath.Should().NotBeNull();

            // Act
            var result = await _mediaService.DeleteFileAsync(savedPath!);

            // Assert
            result.Succeeded.Should().BeTrue();
            var files = await _mediaService.GetFilesAsync(new MediaLibraryQuery { Folder = "general" });
            files.Should().BeEmpty();
        }

        [Fact]
        public async Task GetFileUsagesAsync_UnusedFile_ReturnsEmptyList()
        {
            // Act
            var usages = await _mediaService.GetFileUsagesAsync("/uploads/general/unused-image.png");

            // Assert
            usages.Should().BeEmpty();
        }

        [Fact]
        public async Task GetFileUsagesAsync_UsedAcrossMultipleEntities_ReturnsAllReferences()
        {
            // Arrange
            var testImagePath = "/uploads/brands/shared-logo.png";

            _db.BrandProfiles.Add(new BrandProfile
            {
                NameEn = "Daleel HQ",
                Slug = "daleel-hq",
                LogoPath = testImagePath,
                LogoDarkPath = testImagePath,
                IsPublished = true
            });

            _db.CmsServices.Add(new CmsService
            {
                BrandId = 1,
                NameEn = "Neural Engine",
                Slug = "neural-engine",
                ImagePath = testImagePath,
                IsPublished = true
            });

            _db.CmsProjects.Add(new CmsProject
            {
                BrandId = 1,
                NameEn = "Vision 2030 Portal",
                Slug = "vision-2030-portal",
                ImagePath = testImagePath,
                IsPublished = true
            });

            _db.TeamMembers.Add(new TeamMember
            {
                BrandId = 1,
                NameEn = "Sarah Connor",
                Slug = "sarah-connor",
                PhotoPath = testImagePath,
                IsPublished = true
            });

            _db.Articles.Add(new Article
            {
                TitleEn = "The Future of AI",
                Slug = "future-ai",
                CoverImagePath = testImagePath,
                BodyHtmlEn = "<p>Intro</p>",
                IsPublished = true
            });

            _db.PageSections.Add(new PageSection
            {
                PageKey = "home",
                SectionKey = "hero_banner",
                DataType = "ImagePath",
                ValueEn = testImagePath,
                ValueAr = testImagePath
            });

            await _db.SaveChangesAsync();

            // Act
            var usages = await _mediaService.GetFileUsagesAsync(testImagePath);

            // Assert
            usages.Should().HaveCount(7); // Brand Logo + Brand DarkLogo + Service + Project + Team + Article + PageSection
            usages.Select(u => u.EntityType).Should().Contain(new[] { "Brand", "Service", "Project", "Team Member", "Article", "Page Section" });
        }

        [Fact]
        public async Task DeleteFileAsync_WhenFileInUse_DeletesFileAndUnlinksFromEntities()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var savedPath = await _storageService.SaveFileAsync(stream, "service-img.png", "services");
            savedPath.Should().NotBeNull();

            var service = new CmsService
            {
                BrandId = 1,
                NameEn = "AI Workflow",
                NameAr = "سير عمل الذكاء",
                Slug = "ai-workflow",
                ImagePath = savedPath,
                IsPublished = true
            };
            _db.CmsServices.Add(service);

            var project = new CmsProject
            {
                BrandId = 1,
                NameEn = "Enterprise Project",
                NameAr = "مشروع مؤسسي",
                Slug = "enterprise-project",
                ImagePath = savedPath,
                IsPublished = true
            };
            _db.CmsProjects.Add(project);

            await _db.SaveChangesAsync();

            // Act
            var result = await _mediaService.DeleteFileAsync(savedPath!);

            // Assert
            result.Succeeded.Should().BeTrue();

            var updatedService = await _db.CmsServices.FindAsync(service.Id);
            updatedService.Should().NotBeNull();
            updatedService!.ImagePath.Should().BeNull();

            var updatedProject = await _db.CmsProjects.FindAsync(project.Id);
            updatedProject.Should().NotBeNull();
            updatedProject!.ImagePath.Should().BeNull();
        }
    }
}
