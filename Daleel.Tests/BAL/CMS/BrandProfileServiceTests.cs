using Daleel.BAL.Models;
using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Daleel.Tests.Common;

namespace Daleel.Tests.BAL.CMS
{
    public class BrandProfileServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly BrandProfileService _service;

        public BrandProfileServiceTests()
        {
            _db = TestDbContextFactory.Create();
            _service = new BrandProfileService(_db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ValidInput_CreatesBrandProfile()
        {
            // Arrange
            var input = new BrandProfileInput
            {
                NameEn = "Daleel Global Vision",
                NameAr = "رؤية دليل العالمية",
                TaglineEn = "Next-Gen E-Commerce Solutions",
                TaglineAr = "حلول التجارة الإلكترونية المبتكرة",
                DescriptionEn = "Leading technology provider",
                DescriptionAr = "مزود رائد للحلول التقنية",
                Email = "info@daleel.sa",
                Phone = "+966500000000",
                IsPublished = true
            };

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Succeeded.Should().BeTrue();
            var created = result.Value!;
            created.Id.Should().BeGreaterThan(0);
            created.Slug.Should().Be("daleel-global-vision");
            created.IsPublished.Should().BeTrue();

            var inDb = await _db.BrandProfiles.FindAsync(created.Id);
            inDb.Should().NotBeNull();
            inDb!.NameEn.Should().Be("Daleel Global Vision");
        }

        [Fact]
        public async Task GetBySlugAsync_ReturnsCorrectBrand()
        {
            // Arrange
            var brand = new BrandProfile
            {
                NameEn = "Neurix Agency",
                NameAr = "وكالة نيوريكس",
                Slug = "neurix-agency",
                IsPublished = true
            };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.GetBySlugAsync("neurix-agency");

            // Assert
            result.Should().NotBeNull();
            result!.NameEn.Should().Be("Neurix Agency");
        }

        [Fact]
        public async Task GetAllAsync_WhenOnlyPublished_FiltersCorrectly()
        {
            // Arrange
            _db.BrandProfiles.Add(new BrandProfile { NameEn = "Published Brand", NameAr = "براند منشور", Slug = "pub", IsPublished = true });
            _db.BrandProfiles.Add(new BrandProfile { NameEn = "Draft Brand", NameAr = "براند مسودة", Slug = "draft", IsPublished = false });
            await _db.SaveChangesAsync();

            // Act
            var all = await _service.GetAllAsync(onlyPublished: false);
            var publishedOnly = await _service.GetAllAsync(onlyPublished: true);

            // Assert
            all.Should().HaveCount(2);
            publishedOnly.Should().HaveCount(1);
            publishedOnly[0].Slug.Should().Be("pub");
        }

        [Fact]
        public async Task UpdateAsync_ModifiesFields()
        {
            // Arrange
            var brand = new BrandProfile
            {
                NameEn = "Original Brand",
                NameAr = "براند أصلي",
                Slug = "orig-brand",
                IsPublished = true
            };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            var updateInput = new BrandProfileInput
            {
                NameEn = "Updated Brand",
                NameAr = "براند محدث",
                TaglineEn = "New Tagline",
                TaglineAr = "شعار جديد",
                IsPublished = false
            };

            // Act
            var result = await _service.UpdateAsync(brand.Id, updateInput);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value!.NameEn.Should().Be("Updated Brand");
            result.Value.IsPublished.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_SoftDeletesBrandInDb()
        {
            // Arrange
            var brand = new BrandProfile
            {
                NameEn = "Brand To Delete",
                NameAr = "براند للحذف",
                Slug = "brand-to-delete"
            };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(brand.Id);

            // Assert
            result.Succeeded.Should().BeTrue();
            var inDb = await _db.BrandProfiles.FindAsync(brand.Id);
            inDb.Should().NotBeNull();
            inDb!.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task SeedDefaultBrandsIfEmptyAsync_SeedsDaleelAndNeurixWhenEmpty()
        {
            // Act
            await _service.SeedDefaultBrandsIfEmptyAsync();

            // Assert
            var count = _db.BrandProfiles.Count();
            count.Should().BeGreaterThanOrEqualTo(2);

            var daleel = await _service.GetBySlugAsync("daleel");
            daleel.Should().NotBeNull();
            // Running a second time should not duplicate
            await _service.SeedDefaultBrandsIfEmptyAsync();
            _db.BrandProfiles.Count().Should().Be(count);
        }

        [Fact]
        public async Task CreateAsync_WithColorsAndWebsiteSettings_PersistsAllProperties()
        {
            // Arrange
            var input = new BrandProfileInput
            {
                NameEn = "Daleel AI",
                NameAr = "دليل للذكاء الاصطناعي",
                PrimaryColor = "#00B2EC",
                SecondaryColor = "#10B981",
                AccentColor = "#F9A01B",
                FaviconPath = "/favicon.svg",
                LogoDarkPath = "/assets/Logo/Daleel-dark.png",
                MetaTitleEn = "Daleel Enterprise",
                MetaTitleAr = "دليل للمؤسسات",
                MetaDescriptionEn = "Enterprise AI OS",
                MetaDescriptionAr = "منظومة ذكاء الأعمال",
                GoogleAnalyticsId = "G-1234567890",
                LinkedInUrl = "https://linkedin.com/company/daleel",
                TwitterUrl = "https://x.com/daleel_ai",
                FacebookUrl = "https://facebook.com/daleel",
                InstagramUrl = "https://instagram.com/daleel",
                IsPublished = true
            };

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Succeeded.Should().BeTrue();
            var detail = result.Value!;
            detail.PrimaryColor.Should().Be("#00B2EC");
            detail.SecondaryColor.Should().Be("#10B981");
            detail.AccentColor.Should().Be("#F9A01B");
            detail.FaviconPath.Should().Be("/favicon.svg");
            detail.LogoDarkPath.Should().Be("/assets/Logo/Daleel-dark.png");
            detail.MetaTitleEn.Should().Be("Daleel Enterprise");
            detail.MetaTitleAr.Should().Be("دليل للمؤسسات");
            detail.MetaDescriptionEn.Should().Be("Enterprise AI OS");
            detail.MetaDescriptionAr.Should().Be("منظومة ذكاء الأعمال");
            detail.GoogleAnalyticsId.Should().Be("G-1234567890");
            detail.LinkedInUrl.Should().Be("https://linkedin.com/company/daleel");
            detail.TwitterUrl.Should().Be("https://x.com/daleel_ai");
            detail.FacebookUrl.Should().Be("https://facebook.com/daleel");
            detail.InstagramUrl.Should().Be("https://instagram.com/daleel");
        }
    }
}
