using Daleel.BAL.Models;
using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Daleel.Tests.Common;

namespace Daleel.Tests.BAL.CMS
{
    public class TestimonialServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly TestimonialService _service;

        public TestimonialServiceTests()
        {
            _db = TestDbContextFactory.Create();
            _service = new TestimonialService(_db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ValidInput_CreatesTestimonial()
        {
            // Arrange
            var brand = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            var input = new TestimonialInput
            {
                BrandId = brand.Id,
                CustomerNameEn = "Sultan Al-Hokair",
                CustomerNameAr = "سلطان الحكير",
                CompanyNameEn = "Al-Hokair Holding",
                CompanyNameAr = "مجموعة الحكير القابضة",
                RoleTitleEn = "Managing Director",
                RoleTitleAr = "العضو المنتدب",
                ContentEn = "Daleel transformed our digital retail presence beyond expectations.",
                ContentAr = "حولت منصة دليل تواجدنا الرقمي في قطاع التجزئة بما يفوق التوقعات.",
                Rating = 5,
                IsPublished = true,
                DisplayOrder = 1
            };

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Succeeded.Should().BeTrue();
            var created = result.Value!;
            created.Id.Should().BeGreaterThan(0);
            created.CustomerNameEn.Should().Be("Sultan Al-Hokair");
            created.Rating.Should().Be(5);

            var inDb = await _db.Testimonials.FindAsync(created.Id);
            inDb.Should().NotBeNull();
            inDb!.CustomerNameEn.Should().Be("Sultan Al-Hokair");
        }

        [Fact]
        public async Task GetByBrandSlugAsync_FiltersByBrandAndPublished()
        {
            // Arrange
            var brand1 = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            var brand2 = new BrandProfile { NameEn = "Neurix", NameAr = "نيوريكس", Slug = "neurix" };
            _db.BrandProfiles.AddRange(brand1, brand2);
            await _db.SaveChangesAsync();

            _db.Testimonials.AddRange(
                new Testimonial { BrandId = brand1.Id, CustomerNameEn = "T1", CustomerNameAr = "ع1", ContentEn = "Good", ContentAr = "جيد", Rating = 5, IsPublished = true, DisplayOrder = 1 },
                new Testimonial { BrandId = brand1.Id, CustomerNameEn = "T2", CustomerNameAr = "ع2", ContentEn = "Draft", ContentAr = "مسودة", Rating = 4, IsPublished = false, DisplayOrder = 2 },
                new Testimonial { BrandId = brand2.Id, CustomerNameEn = "T3", CustomerNameAr = "ع3", ContentEn = "Neurix Review", ContentAr = "تقييم نيوريكس", Rating = 5, IsPublished = true, DisplayOrder = 1 }
            );
            await _db.SaveChangesAsync();

            // Act
            var daleelPublished = await _service.GetByBrandSlugAsync("daleel", onlyPublished: true);

            // Assert
            daleelPublished.Should().HaveCount(1);
            daleelPublished[0].CustomerNameEn.Should().Be("T1");
        }

        [Fact]
        public async Task UpdateAsync_UpdatesFields()
        {
            // Arrange
            var brand = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            var testimonial = new Testimonial
            {
                BrandId = brand.Id,
                CustomerNameEn = "Original Name",
                CustomerNameAr = "اسم أصلي",
                ContentEn = "Old quote",
                ContentAr = "اقتباس قديم",
                Rating = 4,
                IsPublished = true
            };
            _db.Testimonials.Add(testimonial);
            await _db.SaveChangesAsync();

            var updateInput = new TestimonialInput
            {
                BrandId = brand.Id,
                CustomerNameEn = "Updated Name",
                CustomerNameAr = "اسم محدث",
                ContentEn = "New quote",
                ContentAr = "اقتباس جديد",
                Rating = 5,
                IsPublished = true
            };

            // Act
            var result = await _service.UpdateAsync(testimonial.Id, updateInput);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value!.CustomerNameEn.Should().Be("Updated Name");
            result.Value.Rating.Should().Be(5);
        }

        [Fact]
        public async Task DeleteAsync_SoftDeletesTestimonial()
        {
            // Arrange
            var brand = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            var testimonial = new Testimonial
            {
                BrandId = brand.Id,
                CustomerNameEn = "To Delete",
                CustomerNameAr = "للحذف",
                ContentEn = "Delete",
                ContentAr = "حذف"
            };
            _db.Testimonials.Add(testimonial);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(testimonial.Id);

            // Assert
            result.Succeeded.Should().BeTrue();
            var inDb = await _db.Testimonials.FindAsync(testimonial.Id);
            inDb.Should().NotBeNull();
            inDb!.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task SeedDefaultTestimonialsIfEmptyAsync_SeedsTestimonials()
        {
            // Arrange
            var brandService = new BrandProfileService(_db);
            await brandService.SeedDefaultBrandsIfEmptyAsync();

            // Act
            await _service.SeedDefaultTestimonialsIfEmptyAsync();

            // Assert
            _db.Testimonials.Count().Should().BeGreaterThan(0);
        }
    }
}
