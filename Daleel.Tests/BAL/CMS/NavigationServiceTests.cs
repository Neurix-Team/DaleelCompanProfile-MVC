using Daleel.BAL.Models;
using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Daleel.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;

namespace Daleel.Tests.BAL.CMS
{
    public class NavigationServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly NavigationService _service;

        public NavigationServiceTests()
        {
            _db = TestDbContextFactory.Create();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _service = new NavigationService(_db, _cache);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
            _cache.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ValidHeaderInput_CreatesNavigationLink()
        {
            // Arrange
            var input = new NavigationLinkInput
            {
                Location = NavigationLocation.Header,
                LabelEn = "Features",
                LabelAr = "الميزات",
                Url = "/features",
                SortOrder = 10,
                IsActive = true,
                OpenInNewTab = false
            };

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Id.Should().BeGreaterThan(0);
            result.Value.LabelEn.Should().Be("Features");
            result.Value.LabelAr.Should().Be("الميزات");
            result.Value.Url.Should().Be("/features");
            result.Value.Location.Should().Be(NavigationLocation.Header);
            result.Value.Section.Should().BeNull();
            result.Value.SortOrder.Should().Be(10);
            result.Value.IsActive.Should().BeTrue();

            var inDb = await _db.NavigationLinks.FindAsync(result.Value.Id);
            inDb.Should().NotBeNull();
            inDb!.LabelEn.Should().Be("Features");
        }

        [Fact]
        public async Task CreateAsync_ValidFooterInput_CreatesNavigationLinkWithSection()
        {
            // Arrange
            var input = new NavigationLinkInput
            {
                Location = NavigationLocation.Footer,
                Section = FooterSection.Platform,
                LabelEn = "Developers API",
                LabelAr = "واجهة المطورين",
                Url = "/api",
                SortOrder = 1,
                IsActive = true,
                OpenInNewTab = true
            };

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value!.Location.Should().Be(NavigationLocation.Footer);
            result.Value.Section.Should().Be(FooterSection.Platform);
            result.Value.OpenInNewTab.Should().BeTrue();
        }

        [Fact]
        public async Task CreateAsync_MissingLabels_FailsValidation()
        {
            // Arrange
            var input = new NavigationLinkInput
            {
                Location = NavigationLocation.Header,
                LabelEn = "",
                LabelAr = " ",
                Url = ""
            };

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task CreateAsync_FooterWithoutSection_FailsValidation()
        {
            // Arrange
            var input = new NavigationLinkInput
            {
                Location = NavigationLocation.Footer,
                Section = null,
                LabelEn = "Help Center",
                LabelAr = "مركز المساعدة",
                Url = "/help"
            };

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Field == nameof(input.Section));
        }

        [Fact]
        public async Task UpdateAsync_ValidInput_UpdatesNavigationLink()
        {
            // Arrange
            var link = new NavigationLink
            {
                Location = NavigationLocation.Header,
                LabelEn = "Old Name",
                LabelAr = "الاسم القديم",
                Url = "/old",
                SortOrder = 1,
                IsActive = true
            };
            _db.NavigationLinks.Add(link);
            await _db.SaveChangesAsync();

            var updateInput = new NavigationLinkInput
            {
                Location = NavigationLocation.Header,
                LabelEn = "New Name",
                LabelAr = "الاسم الجديد",
                Url = "/new",
                SortOrder = 2,
                IsActive = false,
                OpenInNewTab = true
            };

            // Act
            var result = await _service.UpdateAsync(link.Id, updateInput);

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Value!.LabelEn.Should().Be("New Name");
            result.Value.IsActive.Should().BeFalse();
            result.Value.OpenInNewTab.Should().BeTrue();

            var inDb = await _db.NavigationLinks.FindAsync(link.Id);
            inDb!.LabelEn.Should().Be("New Name");
            inDb.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_NonExistentId_ReturnsNotFound()
        {
            // Act
            var result = await _service.UpdateAsync(99999, new NavigationLinkInput
            {
                Location = NavigationLocation.Header,
                LabelEn = "Test",
                LabelAr = "اختبار",
                Url = "/test"
            });

            // Assert
            result.NotFound.Should().BeTrue();
            result.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_ExistingId_DeletesLink()
        {
            // Arrange
            var link = new NavigationLink
            {
                Location = NavigationLocation.Header,
                LabelEn = "To Delete",
                LabelAr = "للحذف",
                Url = "/delete",
                SortOrder = 1
            };
            _db.NavigationLinks.Add(link);
            await _db.SaveChangesAsync();

            // Act
            var result = await _service.DeleteAsync(link.Id);

            // Assert
            result.Succeeded.Should().BeTrue();
            var inDb = await _db.NavigationLinks.FindAsync(link.Id);
            inDb.Should().BeNull();
        }

        [Fact]
        public async Task ToggleActiveAsync_TogglesIsActiveState()
        {
            // Arrange
            var link = new NavigationLink
            {
                Location = NavigationLocation.Header,
                LabelEn = "Toggle Me",
                LabelAr = "تبديل",
                Url = "/toggle",
                IsActive = true
            };
            _db.NavigationLinks.Add(link);
            await _db.SaveChangesAsync();

            // Act 1: Toggle off
            var res1 = await _service.ToggleActiveAsync(link.Id);
            res1.Succeeded.Should().BeTrue();
            res1.Value.Should().BeFalse();

            // Act 2: Toggle on
            var res2 = await _service.ToggleActiveAsync(link.Id);
            res2.Succeeded.Should().BeTrue();
            res2.Value.Should().BeTrue();
        }

        [Fact]
        public async Task ReorderAsync_UpdatesMultipleLinksSortOrders()
        {
            // Arrange
            var link1 = new NavigationLink { Location = NavigationLocation.Header, LabelEn = "L1", LabelAr = "ر1", Url = "/1", SortOrder = 1 };
            var link2 = new NavigationLink { Location = NavigationLocation.Header, LabelEn = "L2", LabelAr = "ر2", Url = "/2", SortOrder = 2 };
            var link3 = new NavigationLink { Location = NavigationLocation.Header, LabelEn = "L3", LabelAr = "ر3", Url = "/3", SortOrder = 3 };

            _db.NavigationLinks.AddRange(link1, link2, link3);
            await _db.SaveChangesAsync();

            // Act: Reverse order [link3, link1, link2]
            var result = await _service.ReorderAsync(new List<int> { link3.Id, link1.Id, link2.Id });

            // Assert
            result.Succeeded.Should().BeTrue();

            var u1 = await _db.NavigationLinks.FindAsync(link1.Id);
            var u2 = await _db.NavigationLinks.FindAsync(link2.Id);
            var u3 = await _db.NavigationLinks.FindAsync(link3.Id);

            u3!.SortOrder.Should().Be(1);
            u1!.SortOrder.Should().Be(2);
            u2!.SortOrder.Should().Be(3);
        }

        [Fact]
        public async Task GetActiveHeaderLinksAsync_FiltersOutInactive_OrdersBySortOrder()
        {
            // Arrange
            var link1 = new NavigationLink { Location = NavigationLocation.Header, LabelEn = "Second", LabelAr = "الثاني", Url = "/2", SortOrder = 2, IsActive = true };
            var link2 = new NavigationLink { Location = NavigationLocation.Header, LabelEn = "Inactive", LabelAr = "معطل", Url = "/x", SortOrder = 1, IsActive = false };
            var link3 = new NavigationLink { Location = NavigationLocation.Header, LabelEn = "First", LabelAr = "الأول", Url = "/1", SortOrder = 1, IsActive = true };
            var footerLink = new NavigationLink { Location = NavigationLocation.Footer, Section = FooterSection.Company, LabelEn = "Footer", LabelAr = "فوتر", Url = "/f", SortOrder = 1, IsActive = true };

            _db.NavigationLinks.AddRange(link1, link2, link3, footerLink);
            await _db.SaveChangesAsync();

            // Act
            var activeHeader = await _service.GetActiveHeaderLinksAsync();

            // Assert
            activeHeader.Should().HaveCount(2);
            activeHeader[0].LabelEn.Should().Be("First");
            activeHeader[1].LabelEn.Should().Be("Second");
        }

        [Fact]
        public async Task GetActiveFooterLinksGroupedAsync_GroupsBySection_FiltersOutInactive()
        {
            // Arrange
            var p1 = new NavigationLink { Location = NavigationLocation.Footer, Section = FooterSection.Platform, LabelEn = "P1", LabelAr = "م1", Url = "/p1", SortOrder = 1, IsActive = true };
            var pInactive = new NavigationLink { Location = NavigationLocation.Footer, Section = FooterSection.Platform, LabelEn = "PIn", LabelAr = "معطل", Url = "/pin", SortOrder = 2, IsActive = false };
            var c1 = new NavigationLink { Location = NavigationLocation.Footer, Section = FooterSection.Company, LabelEn = "C1", LabelAr = "ش1", Url = "/c1", SortOrder = 1, IsActive = true };

            _db.NavigationLinks.AddRange(p1, pInactive, c1);
            await _db.SaveChangesAsync();

            // Act
            var grouped = await _service.GetActiveFooterLinksGroupedAsync();

            // Assert
            grouped[FooterSection.Platform].Should().HaveCount(1);
            grouped[FooterSection.Platform][0].LabelEn.Should().Be("P1");

            grouped[FooterSection.Company].Should().HaveCount(1);
            grouped[FooterSection.Company][0].LabelEn.Should().Be("C1");

            grouped[FooterSection.StayInformed].Should().BeEmpty();
        }

        [Fact]
        public async Task SeedDefaultLinksIfEmptyAsync_SeedsExact16Links_IsIdempotent()
        {
            // Act 1: First seed
            await _service.SeedDefaultLinksIfEmptyAsync();

            // Assert
            var all = await _service.GetAllAsync();
            all.Should().HaveCount(16);

            var header = all.Where(l => l.Location == NavigationLocation.Header).ToList();
            header.Should().HaveCount(7);
            header.Select(h => h.LabelEn).Should().ContainInOrder("Home", "Platforms", "Shop", "Trust & Governance", "About Us", "Insights & News", "Contact Us");
            header.Select(h => h.Url).Should().ContainInOrder("/", "/Home/Platforms", "/Home/Shop", "/Home/Trust", "/Home/About", "/Home/Blog", "/Home/Contact");

            var platform = all.Where(l => l.Location == NavigationLocation.Footer && l.Section == FooterSection.Platform).ToList();
            platform.Should().HaveCount(5);
            platform.Select(p => p.LabelEn).Should().ContainInOrder("Solutions", "Intelligence Feed", "Risk Dashboard", "API Access", "Integrations");
            platform.Should().OnlyContain(p => !p.IsActive);
            platform.Should().OnlyContain(p => p.Url == "/Home/Soon");

            var company = all.Where(l => l.Location == NavigationLocation.Footer && l.Section == FooterSection.Company).ToList();
            company.Should().HaveCount(4);
            company.Select(c => c.LabelEn).Should().ContainInOrder("About Us", "Blog & Insights", "Trust & Governance", "Contact Us");
            company.Select(c => c.Url).Should().ContainInOrder("/Home/About", "/Home/Blog", "/Home/Trust", "/Home/Contact");
            company.Should().OnlyContain(c => c.IsActive);

            // Act 2: Seed again (should not duplicate)
            await _service.SeedDefaultLinksIfEmptyAsync();

            var allAfter = await _service.GetAllAsync();
            allAfter.Should().HaveCount(16);
        }
    }
}
