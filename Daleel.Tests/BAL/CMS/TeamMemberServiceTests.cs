using Daleel.BAL.Models;
using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Daleel.Tests.Common;

namespace Daleel.Tests.BAL.CMS
{
    public class TeamMemberServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly TeamMemberService _service;

        public TeamMemberServiceTests()
        {
            _db = TestDbContextFactory.Create();
            _service = new TeamMemberService(_db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ValidInput_CreatesTeamMember()
        {
            // Arrange
            var brand = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            var input = new TeamMemberInput
            {
                BrandId = brand.Id,
                NameEn = "Fahad Al-Otaibi",
                NameAr = "فهد العتيبي",
                TitleEn = "Chief Technology Officer",
                TitleAr = "الرئيس التنفيذي للتقنية",
                BioEn = "15+ years in cloud architecture and enterprise e-commerce systems.",
                BioAr = "أكثر من 15 عاماً في البنية التحتية السحابية وأنظمة التجارة الإلكترونية.",
                Email = "f.otaibi@daleel.sa",
                LinkedInUrl = "https://linkedin.com/in/fotaibi",
                DisplayOrder = 1,
                IsPublished = true
            };

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Succeeded.Should().BeTrue();
            var created = result.Value!;
            created.Id.Should().BeGreaterThan(0);
            created.Slug.Should().Be("fahad-al-otaibi");
            created.NameEn.Should().Be("Fahad Al-Otaibi");

            var inDb = await _db.TeamMembers.FindAsync(created.Id);
            inDb.Should().NotBeNull();
            inDb!.TitleEn.Should().Be("Chief Technology Officer");
        }

        [Fact]
        public async Task GetByBrandSlugAsync_FiltersByBrand()
        {
            // Arrange
            var brand1 = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            var brand2 = new BrandProfile { NameEn = "Neurix", NameAr = "نيوريكس", Slug = "neurix" };
            _db.BrandProfiles.AddRange(brand1, brand2);
            await _db.SaveChangesAsync();

            _db.TeamMembers.AddRange(
                new TeamMember { BrandId = brand1.Id, NameEn = "Member 1", NameAr = "عضو 1", TitleEn = "Dev", TitleAr = "مطور", Slug = "m1", IsPublished = true, DisplayOrder = 1 },
                new TeamMember { BrandId = brand2.Id, NameEn = "Member 2", NameAr = "عضو 2", TitleEn = "Lead", TitleAr = "قائد", Slug = "m2", IsPublished = true, DisplayOrder = 1 }
            );
            await _db.SaveChangesAsync();

            // Act
            var daleelTeam = await _service.GetByBrandSlugAsync("daleel", onlyPublished: true);

            // Assert
            daleelTeam.Should().HaveCount(1);
            daleelTeam[0].Slug.Should().Be("m1");
        }

        [Fact]
        public async Task UpdateAsync_UpdatesTeamMember()
        {
            // Arrange
            var brand = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            var member = new TeamMember
            {
                BrandId = brand.Id,
                NameEn = "Original Name",
                NameAr = "اسم أصلي",
                TitleEn = "Junior",
                TitleAr = "مبتدئ",
                Slug = "orig-member",
                IsPublished = true
            };
            _db.TeamMembers.Add(member);
            await _db.SaveChangesAsync();

            var updateInput = new TeamMemberInput
            {
                BrandId = brand.Id,
                NameEn = "Updated Name",
                NameAr = "اسم محدث",
                TitleEn = "Senior",
                TitleAr = "خبير",
                IsPublished = true
            };

            // Act
            var result = await _service.UpdateAsync(member.Id, updateInput);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value!.NameEn.Should().Be("Updated Name");
            result.Value.TitleEn.Should().Be("Senior");
        }

        [Fact]
        public async Task DeleteAsync_SoftDeletesTeamMember()
        {
            // Arrange
            var brand = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            var member = new TeamMember
            {
                BrandId = brand.Id,
                NameEn = "Delete Me",
                NameAr = "احذفني",
                TitleEn = "Temp",
                TitleAr = "مؤقت",
                Slug = "delete-me"
            };
            _db.TeamMembers.Add(member);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(member.Id);

            // Assert
            result.Succeeded.Should().BeTrue();
            var inDb = await _db.TeamMembers.FindAsync(member.Id);
            inDb.Should().NotBeNull();
            inDb!.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task SeedDefaultTeamIfEmptyAsync_SeedsDefaultTeam()
        {
            // Arrange
            var brandService = new BrandProfileService(_db);
            await brandService.SeedDefaultBrandsIfEmptyAsync();

            // Act
            await _service.SeedDefaultTeamIfEmptyAsync();

            // Assert
            _db.TeamMembers.Count().Should().BeGreaterThan(0);
        }
    }
}
