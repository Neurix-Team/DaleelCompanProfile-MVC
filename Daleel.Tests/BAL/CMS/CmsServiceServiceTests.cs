using Daleel.BAL.Models;
using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Daleel.Tests.Common;

namespace Daleel.Tests.BAL.CMS
{
    public class CmsServiceServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly CmsServiceService _service;

        public CmsServiceServiceTests()
        {
            _db = TestDbContextFactory.Create();
            _service = new CmsServiceService(_db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ValidInput_CreatesCmsService()
        {
            // Arrange
            var brand = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel", IsPublished = true };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            var input = new CmsServiceInput
            {
                BrandId = brand.Id,
                NameEn = "E-Commerce Development",
                NameAr = "تطوير المتاجر الإلكترونية",
                ShortDescEn = "Custom high-scale online storefronts.",
                ShortDescAr = "متاجر إلكترونية مخصصة وقابلة للتوسع.",
                DescriptionEn = "Full end-to-end e-commerce development.",
                DescriptionAr = "تطوير شامل للمتاجر الإلكترونية من البداية حتى الإطلاق.",
                IconName = "bi bi-shop",
                DisplayOrder = 1,
                IsPublished = true
            };

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Succeeded.Should().BeTrue();
            var created = result.Value!;
            created.Id.Should().BeGreaterThan(0);
            created.Slug.Should().Be("e-commerce-development");
            created.BrandSlug.Should().Be("daleel");

            var inDb = await _db.CmsServices.FindAsync(created.Id);
            inDb.Should().NotBeNull();
            inDb!.NameEn.Should().Be("E-Commerce Development");
        }

        [Fact]
        public async Task GetByBrandSlugAsync_FiltersByBrandAndPublishedStatus()
        {
            // Arrange
            var brand1 = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel", IsPublished = true };
            var brand2 = new BrandProfile { NameEn = "Neurix", NameAr = "نيوريكس", Slug = "neurix", IsPublished = true };
            _db.BrandProfiles.AddRange(brand1, brand2);
            await _db.SaveChangesAsync();

            _db.CmsServices.AddRange(
                new CmsService { BrandId = brand1.Id, NameEn = "S1 Daleel Pub", NameAr = "خ1", Slug = "s1", IsPublished = true, DisplayOrder = 1 },
                new CmsService { BrandId = brand1.Id, NameEn = "S2 Daleel Draft", NameAr = "خ2", Slug = "s2", IsPublished = false, DisplayOrder = 2 },
                new CmsService { BrandId = brand2.Id, NameEn = "S3 Neurix Pub", NameAr = "خ3", Slug = "s3", IsPublished = true, DisplayOrder = 1 }
            );
            await _db.SaveChangesAsync();

            // Act
            var daleelPub = await _service.GetByBrandSlugAsync("daleel", onlyPublished: true);
            var daleelAll = await _service.GetByBrandSlugAsync("daleel", onlyPublished: false);
            var neurixPub = await _service.GetByBrandSlugAsync("neurix", onlyPublished: true);

            // Assert
            daleelPub.Should().HaveCount(1);
            daleelPub[0].Slug.Should().Be("s1");

            daleelAll.Should().HaveCount(2);

            neurixPub.Should().HaveCount(1);
            neurixPub[0].Slug.Should().Be("s3");
        }

        [Fact]
        public async Task UpdateAsync_UpdatesServiceFields()
        {
            // Arrange
            var brand = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            var service = new CmsService
            {
                BrandId = brand.Id,
                NameEn = "Original Srv",
                NameAr = "خدمة اصلية",
                Slug = "orig-srv",
                IsPublished = true
            };
            _db.CmsServices.Add(service);
            await _db.SaveChangesAsync();

            var updateInput = new CmsServiceInput
            {
                BrandId = brand.Id,
                NameEn = "Updated Srv",
                NameAr = "خدمة محدثة",
                ShortDescEn = "New short desc",
                ShortDescAr = "وصف قصير جديد",
                IsPublished = false
            };

            // Act
            var result = await _service.UpdateAsync(service.Id, updateInput);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value!.NameEn.Should().Be("Updated Srv");
            result.Value.IsPublished.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_SoftDeletesService()
        {
            // Arrange
            var brand = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            var service = new CmsService { BrandId = brand.Id, NameEn = "To Delete", NameAr = "للحذف", Slug = "del-srv" };
            _db.CmsServices.Add(service);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(service.Id);

            // Assert
            result.Succeeded.Should().BeTrue();
            var inDb = await _db.CmsServices.FindAsync(service.Id);
            inDb.Should().NotBeNull();
            inDb!.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task SeedDefaultServicesIfEmptyAsync_SeedsDefaultServices()
        {
            // Arrange
            var brandProfileService = new BrandProfileService(_db);
            await brandProfileService.SeedDefaultBrandsIfEmptyAsync();

            // Act
            await _service.SeedDefaultServicesIfEmptyAsync();

            // Assert
            _db.CmsServices.Count().Should().BeGreaterThan(0);
        }
    }
}
