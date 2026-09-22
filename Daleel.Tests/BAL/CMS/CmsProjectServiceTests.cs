using Daleel.BAL.Models;
using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Daleel.Tests.Common;

namespace Daleel.Tests.BAL.CMS
{
    public class CmsProjectServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly CmsProjectService _service;

        public CmsProjectServiceTests()
        {
            _db = TestDbContextFactory.Create();
            _service = new CmsProjectService(_db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ValidInput_CreatesCmsProject()
        {
            // Arrange
            var brand = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            var input = new CmsProjectInput
            {
                BrandId = brand.Id,
                NameEn = "Luxury Fashion E-Commerce",
                NameAr = "متجر أزياء فاخرة",
                ShortDescEn = "Next-gen luxury shopping experience.",
                ShortDescAr = "تجربة تسوق فاخرة للجيل القادم.",
                ClientNameEn = "Al-Nukhba Group",
                ClientNameAr = "مجموعة النخبة",
                IsPublished = true,
                DisplayOrder = 1
            };

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Succeeded.Should().BeTrue();
            var created = result.Value!;
            created.Id.Should().BeGreaterThan(0);
            created.Slug.Should().Be("luxury-fashion-e-commerce");
            created.ClientNameEn.Should().Be("Al-Nukhba Group");

            var inDb = await _db.CmsProjects.FindAsync(created.Id);
            inDb.Should().NotBeNull();
            inDb!.NameEn.Should().Be("Luxury Fashion E-Commerce");
        }

        [Fact]
        public async Task GetByBrandSlugAsync_FiltersCorrectly()
        {
            // Arrange
            var brand1 = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            var brand2 = new BrandProfile { NameEn = "Neurix", NameAr = "نيوريكس", Slug = "neurix" };
            _db.BrandProfiles.AddRange(brand1, brand2);
            await _db.SaveChangesAsync();

            _db.CmsProjects.AddRange(
                new CmsProject { BrandId = brand1.Id, NameEn = "P1 Daleel Pub", NameAr = "م1", Slug = "p1", IsPublished = true },
                new CmsProject { BrandId = brand1.Id, NameEn = "P2 Daleel Draft", NameAr = "م2", Slug = "p2", IsPublished = false },
                new CmsProject { BrandId = brand2.Id, NameEn = "P3 Neurix Pub", NameAr = "م3", Slug = "p3", IsPublished = true }
            );
            await _db.SaveChangesAsync();

            // Act
            var daleelPub = await _service.GetByBrandSlugAsync("daleel", onlyPublished: true);
            var daleelAll = await _service.GetByBrandSlugAsync("daleel", onlyPublished: false);

            // Assert
            daleelPub.Should().HaveCount(1);
            daleelPub[0].Slug.Should().Be("p1");
            daleelAll.Should().HaveCount(2);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesFields()
        {
            // Arrange
            var brand = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            var project = new CmsProject { BrandId = brand.Id, NameEn = "Old Project", NameAr = "مشروع قديم", Slug = "old-prj", IsPublished = true };
            _db.CmsProjects.Add(project);
            await _db.SaveChangesAsync();

            var updateInput = new CmsProjectInput
            {
                BrandId = brand.Id,
                NameEn = "Updated Project",
                NameAr = "مشروع محدث",
                ClientNameEn = "New Client",
                IsPublished = true
            };

            // Act
            var result = await _service.UpdateAsync(project.Id, updateInput);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value!.NameEn.Should().Be("Updated Project");
            result.Value.ClientNameEn.Should().Be("New Client");
        }

        [Fact]
        public async Task DeleteAsync_SoftDeletesProject()
        {
            // Arrange
            var brand = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            var project = new CmsProject { BrandId = brand.Id, NameEn = "To Delete", NameAr = "للحذف", Slug = "del-prj" };
            _db.CmsProjects.Add(project);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(project.Id);

            // Assert
            result.Succeeded.Should().BeTrue();
            var inDb = await _db.CmsProjects.FindAsync(project.Id);
            inDb.Should().NotBeNull();
            inDb!.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task SeedDefaultProjectsIfEmptyAsync_SeedsProjects()
        {
            // Arrange
            var brandProfileService = new BrandProfileService(_db);
            await brandProfileService.SeedDefaultBrandsIfEmptyAsync();

            // Act
            await _service.SeedDefaultProjectsIfEmptyAsync();

            // Assert
            _db.CmsProjects.Count().Should().BeGreaterThan(0);
        }
    }
}
