using Daleel.BAL.Models;
using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Daleel.Tests.Common;
using Microsoft.Extensions.Caching.Memory;

namespace Daleel.Tests.BAL.CMS
{
    public class PageSectionServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly PageSectionService _service;

        public PageSectionServiceTests()
        {
            _db = TestDbContextFactory.Create();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _service = new PageSectionService(_db, _cache);
        }

        public void Dispose()
        {
            _cache.Dispose();
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task GetPageContentMapAsync_ReturnsBilingualMapWithFallback()
        {
            // Arrange
            _db.PageSections.AddRange(
                new PageSection
                {
                    PageKey = "home",
                    SectionKey = "herotitle",
                    ValueEn = "Empowering Modern Retail",
                    ValueAr = "تمكين التجارة الحديثة"
                },
                new PageSection
                {
                    PageKey = "home",
                    SectionKey = "herosubtitleonlyen",
                    ValueEn = "English Subtitle Only",
                    ValueAr = ""
                }
            );
            await _db.SaveChangesAsync();

            // Act - English
            var mapEn = await _service.GetPageContentMapAsync("home", "en");
            // Act - Arabic
            var mapAr = await _service.GetPageContentMapAsync("home", "ar");

            // Assert
            mapEn["herotitle"].Should().Be("Empowering Modern Retail");
            mapAr["herotitle"].Should().Be("تمكين التجارة الحديثة");

            // Fallback for missing Arabic value
            mapAr["herosubtitleonlyen"].Should().Be("English Subtitle Only");
        }

        [Fact]
        public async Task UpsertAsync_UpdatesValueAndInvalidatesCache()
        {
            // Arrange
            var section = new PageSection
            {
                PageKey = "about",
                SectionKey = "mission",
                ValueEn = "Old Mission",
                ValueAr = "مهمة قديمة"
            };
            _db.PageSections.Add(section);
            await _db.SaveChangesAsync();

            // Populate cache
            await _service.GetPageContentMapAsync("about", "en");

            // Act
            var updateInput = new PageSectionInput
            {
                PageKey = "about",
                SectionKey = "mission",
                ValueEn = "New Global Mission",
                ValueAr = "مهمة عالمية جديدة"
            };
            var result = await _service.UpsertAsync(updateInput);

            // Assert
            result.Succeeded.Should().BeTrue();

            // Querying content map should reflect the updated value (cache invalidated)
            var freshMap = await _service.GetPageContentMapAsync("about", "en");
            freshMap["mission"].Should().Be("New Global Mission");
        }

        [Fact]
        public async Task SeedMissingDefaultSectionsAsync_SeedsDefaultPageSections()
        {
            // Act
            await _service.SeedMissingDefaultSectionsAsync();

            // Assert
            _db.PageSections.Count().Should().BeGreaterThan(0);

            // hero_title exists but ships blank: that slot renders the Brand Profile tagline,
            // so a non-empty default would override the brand on every fresh install.
            var homeHero = await _service.GetAsync("home", "hero_title");
            homeHero.Should().NotBeNull();
            homeHero!.ValueEn.Should().BeEmpty();

            // The Trust pair is the one backed by static copy, so it does carry text.
            var trustTitle = await _service.GetAsync("trust", "trust_title");
            trustTitle!.ValueEn.Should().Be("Trust & Governance");
            trustTitle.ValueAr.Should().Be("الثقة والحوكمة");
        }

        [Fact]
        public async Task AlignLegacyPlaceholderSectionsAsync_RetiresUnrenderedPlaceholderCopy()
        {
            // Arrange: a row exactly as the original seeder wrote it.
            _db.PageSections.Add(new PageSection
            {
                PageKey = "home",
                SectionKey = "hero_title",
                ValueEn = "Accelerate Your Business with Daleel Smart Platforms",
                ValueAr = "سرّع وتيرة أعمالك مع منصات دليل الذكية",
                DataType = "Text"
            });
            await _db.SaveChangesAsync();

            // Act
            var changed = await _service.AlignLegacyPlaceholderSectionsAsync();

            // Assert: blanked, so the page falls back to the brand tagline as it always did.
            changed.Should().Be(1);
            (await _service.GetAsync("home", "hero_title"))!.ValueEn.Should().BeEmpty();
        }

        [Fact]
        public async Task AlignLegacyPlaceholderSectionsAsync_LeavesAdminEditedRowsAlone()
        {
            _db.PageSections.Add(new PageSection
            {
                PageKey = "home",
                SectionKey = "hero_title",
                ValueEn = "Our own headline",
                ValueAr = "عنواننا الخاص",
                DataType = "Text"
            });
            await _db.SaveChangesAsync();

            var changed = await _service.AlignLegacyPlaceholderSectionsAsync();

            changed.Should().Be(0);
            (await _service.GetAsync("home", "hero_title"))!.ValueEn.Should().Be("Our own headline");
        }

        [Fact]
        public async Task AlignLegacyPlaceholderSectionsAsync_IsIdempotent()
        {
            _db.PageSections.Add(new PageSection
            {
                PageKey = "trust",
                SectionKey = "trust_title",
                ValueEn = "Enterprise-Grade Security & Compliance",
                ValueAr = "أمان وامتثال متوافق مع أعلى المعايير المؤسسية",
                DataType = "Text"
            });
            await _db.SaveChangesAsync();

            (await _service.AlignLegacyPlaceholderSectionsAsync()).Should().Be(1);
            (await _service.AlignLegacyPlaceholderSectionsAsync()).Should().Be(0);

            (await _service.GetAsync("trust", "trust_title"))!.ValueEn.Should().Be("Trust & Governance");
        }

        [Fact]
        public async Task SeedMissingDefaultSectionsAsync_CoversAllEightEditablePages()
        {
            await _service.SeedMissingDefaultSectionsAsync();

            var pages = await _service.GetDistinctPagesAsync();

            pages.Should().Contain(new[] { "home", "about", "platforms", "trust", "shop", "blog", "contact", "soon" });
        }

        [Fact]
        public async Task SeedMissingDefaultSectionsAsync_AddsNewPagesWithoutTouchingExistingEdits()
        {
            // Arrange: an installation that already holds an edited "home" page and nothing else.
            _db.PageSections.Add(new PageSection
            {
                PageKey = "home",
                SectionKey = "hero_title",
                ValueEn = "Admin edited this",
                ValueAr = "عدّل المدير هذا",
                DataType = "Text"
            });
            await _db.SaveChangesAsync();

            // Act
            await _service.SeedMissingDefaultSectionsAsync();

            // Assert: the edited row survives verbatim...
            var homeHero = await _service.GetAsync("home", "hero_title");
            homeHero!.ValueEn.Should().Be("Admin edited this");

            // ...no default rows were grafted onto the page that already existed...
            var homeSections = await _service.GetSectionsByPageAsync("home");
            homeSections.Should().HaveCount(1);

            // ...and the four new pages were still seeded.
            var pages = await _service.GetDistinctPagesAsync();
            pages.Should().Contain(new[] { "shop", "blog", "contact", "soon" });
        }

        [Fact]
        public async Task DeleteSectionAsync_RemovesSectionAndClearsCache()
        {
            // Arrange
            var section = new PageSection
            {
                PageKey = "pricing",
                SectionKey = "discount_notice",
                ValueEn = "20% off annual plan",
                ValueAr = "خصم 20%"
            };
            _db.PageSections.Add(section);
            await _db.SaveChangesAsync();

            // Populate cache
            await _service.GetPageContentMapAsync("pricing", "en");

            // Act
            var result = await _service.DeleteSectionAsync("pricing", "discount_notice");

            // Assert
            result.Succeeded.Should().BeTrue();
            var inDb = await _service.GetAsync("pricing", "discount_notice");
            inDb.Should().BeNull();

            var freshMap = await _service.GetPageContentMapAsync("pricing", "en");
            freshMap.Should().NotContainKey("discount_notice");
        }

        [Fact]
        public async Task SyncMissingSectionKeysAsync_AddsMissingKeysWithoutOverwritingExisting()
        {
            // Arrange: only 1 section in "home"
            _db.PageSections.Add(new PageSection
            {
                PageKey = "home",
                SectionKey = "hero_title",
                ValueEn = "Custom Title",
                ValueAr = "عنوان مخصص",
                DataType = "Text"
            });
            await _db.SaveChangesAsync();

            // Act
            var syncedCount = await _service.SyncMissingSectionKeysAsync();

            // Assert: new default sections (including images) are added, but existing hero_title remains intact
            syncedCount.Should().BeGreaterThan(0);
            var heroTitle = await _service.GetAsync("home", "hero_title");
            heroTitle!.ValueEn.Should().Be("Custom Title");

            var slideImage = await _service.GetAsync("home", "story_slide1_image");
            slideImage.Should().NotBeNull();
            slideImage!.DataType.Should().Be("ImagePath");
        }

        [Fact]
        public async Task SyncMissingSectionKeysAsync_SyncsAllHomepageSections()
        {
            // Act
            await _service.SyncMissingSectionKeysAsync();

            // Assert
            var homeSections = await _service.GetSectionsByPageAsync("home");
            var keys = homeSections.Select(s => s.SectionKey).ToHashSet();

            var expectedKeys = new[]
            {
                "hero_badge", "hero_title", "hero_title_highlight", "hero_subtitle", "hero_cta_primary", "hero_cta_secondary", "hero_scroll_text",
                "story_slide1_badge", "story_slide1_title", "story_slide1_title_sub", "story_slide1_desc", "story_slide1_stat1", "story_slide1_stat2", "story_slide1_stat3", "story_slide1_image",
                "story_slide2_badge", "story_slide2_title", "story_slide2_title_sub", "story_slide2_desc", "story_slide2_image",
                "story_slide3_badge", "story_slide3_title", "story_slide3_title_sub", "story_slide3_desc", "story_slide3_image",
                "story_slide4_title", "story_slide4_desc", "story_slide4_cta",
                "vision_badge", "vision_title", "vision_title_highlight", "vision_desc", "vision_cta",
                "vision_card1_title", "vision_card1_subtitle", "vision_card1_desc",
                "vision_card2_title", "vision_card2_subtitle", "vision_card2_desc",
                "vision_card3_title", "vision_card3_subtitle", "vision_card3_desc",
                "vision_card4_title", "vision_card4_subtitle", "vision_card4_desc",
                "services_badge", "services_title", "services_title_highlight", "services_subtitle", "services_card_cta",
                "projects_badge", "projects_title", "projects_subtitle",
                "testimonials_badge", "testimonials_title", "testimonials_subtitle",
                "articles_badge", "articles_title", "articles_subtitle", "articles_link_text", "articles_card_cta"
            };

            foreach (var expectedKey in expectedKeys)
            {
                keys.Should().Contain(expectedKey, $"Section key '{expectedKey}' should be synced for page 'home'");
            }
        }
    }
}
