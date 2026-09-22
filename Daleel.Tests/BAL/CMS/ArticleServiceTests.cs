using Daleel.BAL.Models;
using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Daleel.Tests.Common;

namespace Daleel.Tests.BAL.CMS
{
    public class ArticleServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly ArticleService _service;

        public ArticleServiceTests()
        {
            _db = TestDbContextFactory.Create();
            _service = new ArticleService(_db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        [Fact]
        public async Task CreateAsync_ValidInput_CreatesArticleWithSanitizedContentAndUniqueSlug()
        {
            // Arrange
            var input = new ArticleInput
            {
                TitleEn = "E-Commerce Growth Strategies in Saudi Arabia",
                TitleAr = "استراتيجيات نمو التجارة الإلكترونية",
                SummaryEn = "A comprehensive overview of e-commerce expansion in KSA.",
                SummaryAr = "نظرة شاملة حول توسع التجارة الإلكترونية في المملكة.",
                BodyHtmlEn = "<p>Great guide with <script>alert('xss')</script> tips.</p>",
                BodyHtmlAr = "<p>دليل رائع للنمو والتوسع.</p>",
                AuthorName = "Sarah Connor",
                Category = "Strategy",
                Tags = "E-Commerce, Growth, KSA",
                IsPublished = true,
                MetaTitleEn = "E-Commerce Growth 2026",
                MetaDescriptionEn = "Discover how to grow your online store.",
                CanonicalUrl = "https://daleel.sa/articles/growth-strategies"
            };

            // Act
            var result = await _service.CreateAsync(input);

            // Assert
            result.Succeeded.Should().BeTrue();
            var created = result.Value!;
            created.Id.Should().BeGreaterThan(0);
            created.Slug.Should().Be("e-commerce-growth-strategies-in-saudi-arabia");
            created.BodyHtmlEn.Should().NotContain("<script>");
            created.BodyHtmlEn.Should().Contain("<p>Great guide with");
            created.AuthorName.Should().Be("Sarah Connor");
            created.IsPublished.Should().BeTrue();
            created.PublishedAt.Should().NotBeNull();
            created.Tags.Should().Be("E-Commerce, Growth, KSA");
            created.MetaTitleEn.Should().Be("E-Commerce Growth 2026");

            var inDb = await _db.Articles.FindAsync(created.Id);
            inDb.Should().NotBeNull();
            inDb!.Slug.Should().Be("e-commerce-growth-strategies-in-saudi-arabia");
        }

        [Fact]
        public async Task CreateAsync_DuplicateTitle_GeneratesUniqueSlugSuffix()
        {
            // Arrange
            var input1 = new ArticleInput
            {
                TitleEn = "Digital Marketing Trends",
                TitleAr = "اتجاهات التسويق الرقمي",
                BodyHtmlEn = "<p>Article 1</p>",
                BodyHtmlAr = "<p>المقال الأول</p>"
            };
            var input2 = new ArticleInput
            {
                TitleEn = "Digital Marketing Trends",
                TitleAr = "اتجاهات التسويق الرقمي 2",
                BodyHtmlEn = "<p>Article 2</p>",
                BodyHtmlAr = "<p>المقال الثاني</p>"
            };

            // Act
            var res1 = await _service.CreateAsync(input1);
            var res2 = await _service.CreateAsync(input2);

            // Assert
            res1.Succeeded.Should().BeTrue();
            res2.Succeeded.Should().BeTrue();
            res1.Value!.Slug.Should().Be("digital-marketing-trends");
            res2.Value!.Slug.Should().Be("digital-marketing-trends-1");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("<p><br></p>")]
        [InlineData("<p>&nbsp;</p>")]
        [InlineData("<div>\u200B</div>")]
        public async Task CreateAsync_EmptyRichTextContent_ReturnsValidationError(string emptyContent)
        {
            var input = new ArticleInput
            {
                TitleEn = "Article title",
                TitleAr = "Arabic title",
                BodyHtmlEn = emptyContent,
                BodyHtmlAr = "<p>Arabic content</p>"
            };

            var result = await _service.CreateAsync(input);

            result.Succeeded.Should().BeFalse();
            result.Errors.Should().Contain(error =>
                error.Field == nameof(ArticleInput.BodyHtmlEn) &&
                error.Message == "English article content is required.");
            _db.Articles.Should().BeEmpty();
        }

        [Fact]
        public async Task CreateAsync_EmptyArabicRichTextContent_ReturnsValidationError()
        {
            var input = new ArticleInput
            {
                TitleEn = "Article title",
                TitleAr = "Arabic title",
                BodyHtmlEn = "<p>English content</p>",
                BodyHtmlAr = "<p><br></p>"
            };

            var result = await _service.CreateAsync(input);

            result.Succeeded.Should().BeFalse();
            result.Errors.Should().Contain(error =>
                error.Field == nameof(ArticleInput.BodyHtmlAr) &&
                error.Message == "Arabic article content is required.");
            _db.Articles.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchAsync_FiltersByKeywordAndCategory()
        {
            // Arrange
            await _service.CreateAsync(new ArticleInput
            {
                TitleEn = "AI in Retail",
                BodyHtmlEn = "<p>AI retail content</p>",
                BodyHtmlAr = "<p>Arabic content</p>",
                TitleAr = "الذكاء الاصطناعي في التجزئة",
                Category = "Technology",
                Tags = "AI, Retail",
                IsPublished = true
            });

            await _service.CreateAsync(new ArticleInput
            {
                TitleEn = "Logistics Optimization",
                BodyHtmlEn = "<p>Logistics content</p>",
                BodyHtmlAr = "<p>Arabic content</p>",
                TitleAr = "تحسين الخدمات اللوجستية",
                Category = "Operations",
                Tags = "Logistics, Supply",
                IsPublished = true
            });

            // Act
            var techResult = await _service.SearchAsync(new ArticleQuery { Category = "Technology" });
            var queryResult = await _service.SearchAsync(new ArticleQuery { Q = "Logistics" });

            // Assert
            techResult.TotalCount.Should().Be(1);
            techResult.Items[0].TitleEn.Should().Be("AI in Retail");

            queryResult.TotalCount.Should().Be(1);
            queryResult.Items[0].TitleEn.Should().Be("Logistics Optimization");
        }

        [Fact]
        public async Task SearchAsync_FiltersByTag()
        {
            // Arrange
            await _service.CreateAsync(new ArticleInput
            {
                TitleEn = "FinTech Revolution",
                BodyHtmlEn = "<p>FinTech content</p>",
                BodyHtmlAr = "<p>Arabic content</p>",
                TitleAr = "ثورة التكنولوجيا المالية",
                Tags = "Fintech, Banking, Payment",
                IsPublished = true
            });

            await _service.CreateAsync(new ArticleInput
            {
                TitleEn = "Cybersecurity Essentials",
                BodyHtmlEn = "<p>Cybersecurity content</p>",
                BodyHtmlAr = "<p>Arabic content</p>",
                TitleAr = "أساسيات الأمن السيبراني",
                Tags = "Security, Cloud",
                IsPublished = true
            });

            // Act
            var result = await _service.SearchAsync(new ArticleQuery { Tag = "Fintech" });

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items[0].TitleEn.Should().Be("FinTech Revolution");
        }

        [Fact]
        public async Task GetRecentPublishedAsync_ExcludesUnpublishedAndFutureScheduledArticles()
        {
            // Arrange
            // 1. Published and ready
            var art1 = new Article
            {
                TitleEn = "Published Ready",
                TitleAr = "منشور وجاهز",
                Slug = "published-ready",
                BodyHtmlEn = "Content",
                BodyHtmlAr = "المحتوى",
                AuthorName = "Author",
                IsPublished = true,
                PublishedAt = DateTime.UtcNow.AddDays(-1),
                ScheduledPublishAt = null,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            // 2. Draft (unpublished)
            var art2 = new Article
            {
                TitleEn = "Draft Article",
                TitleAr = "مسودة مقال",
                Slug = "draft-article",
                BodyHtmlEn = "Content",
                BodyHtmlAr = "المحتوى",
                AuthorName = "Author",
                IsPublished = false,
                CreatedAt = DateTime.UtcNow
            };

            // 3. Scheduled in the future (tomorrow)
            var art3 = new Article
            {
                TitleEn = "Future Scheduled",
                TitleAr = "مجدول للمستقبل",
                Slug = "future-scheduled",
                BodyHtmlEn = "Content",
                BodyHtmlAr = "المحتوى",
                AuthorName = "Author",
                IsPublished = true,
                ScheduledPublishAt = DateTime.UtcNow.AddDays(2),
                CreatedAt = DateTime.UtcNow
            };

            _db.Articles.AddRange(art1, art2, art3);
            await _db.SaveChangesAsync();

            // Act
            var recent = await _service.GetRecentPublishedAsync(10);

            // Assert
            recent.Should().HaveCount(1);
            recent[0].Slug.Should().Be("published-ready");
        }

        [Fact]
        public async Task GetBySlugAsync_ReturnsArticleAndAllowsDirectFetch()
        {
            // Arrange
            var createRes = await _service.CreateAsync(new ArticleInput
            {
                TitleEn = "High Performance APIs",
                TitleAr = "واجهات برمجة عالية الأداء",
                BodyHtmlEn = "<p>API Details</p>",
                BodyHtmlAr = "<p>تفاصيل الواجهة</p>",
                IsPublished = true
            });

            createRes.Succeeded.Should().BeTrue();

            // Act
            var article = await _service.GetBySlugAsync(createRes.Value!.Slug);

            // Assert
            article.Should().NotBeNull();
            article!.TitleEn.Should().Be("High Performance APIs");
        }

        [Fact]
        public async Task UpdateAsync_UpdatesFieldsAndSanitizesHtml()
        {
            // Arrange
            var createRes = await _service.CreateAsync(new ArticleInput
            {
                TitleEn = "Original Title",
                TitleAr = "العنوان الأصلي",
                BodyHtmlEn = "<p>Original Body</p>",
                BodyHtmlAr = "<p>المحتوى الأصلي</p>",
                Category = "General"
            });

            var updateInput = new ArticleInput
            {
                TitleEn = "Updated Title",
                TitleAr = "العنوان المحدث",
                BodyHtmlEn = "<p>Updated Body <style>body { color: red; }</style></p>",
                BodyHtmlAr = "<p>المحتوى المحدث</p>",
                Category = "Updated Category",
                Tags = "Tag1, Tag2",
                IsPublished = true
            };

            // Act
            var result = await _service.UpdateAsync(createRes.Value!.Id, updateInput);

            // Assert
            result.Succeeded.Should().BeTrue();
            var updated = result.Value!;
            updated.TitleEn.Should().Be("Updated Title");
            updated.Category.Should().Be("Updated Category");
            updated.BodyHtmlEn.Should().NotContain("<style>");
            updated.Tags.Should().Be("Tag1, Tag2");
        }

        [Fact]
        public async Task DeleteAsync_RemovesArticleFromDb()
        {
            // Arrange
            var createRes = await _service.CreateAsync(new ArticleInput
            {
                TitleEn = "To Be Deleted",
                TitleAr = "للحذف",
                BodyHtmlEn = "Content",
                BodyHtmlAr = "المحتوى"
            });

            // Act
            var deleteResult = await _service.DeleteAsync(createRes.Value!.Id);

            // Assert
            deleteResult.Succeeded.Should().BeTrue();
            var inDb = await _db.Articles.FindAsync(createRes.Value!.Id);
            inDb.Should().BeNull();
        }

        [Fact]
        public async Task TogglePublishAsync_TogglesStatusAndSetsPublishedAt()
        {
            // Arrange
            var createRes = await _service.CreateAsync(new ArticleInput
            {
                TitleEn = "Toggle Status Test",
                TitleAr = "اختبار تبديل النشر",
                BodyHtmlEn = "Content",
                BodyHtmlAr = "المحتوى",
                IsPublished = false
            });

            // Act 1: Publish
            var publishedRes = await _service.TogglePublishAsync(createRes.Value!.Id);
            // Act 2: Unpublish
            var unpublishedRes = await _service.TogglePublishAsync(createRes.Value!.Id);

            // Assert
            publishedRes.Succeeded.Should().BeTrue();
            publishedRes.Value.Should().BeTrue();

            unpublishedRes.Succeeded.Should().BeTrue();
            unpublishedRes.Value.Should().BeFalse();
        }

        [Fact]
        public async Task GetDistinctTagsAsync_ReturnsUniqueSortedTags()
        {
            // Arrange
            await _service.CreateAsync(new ArticleInput
            {
                TitleEn = "Article 1",
                BodyHtmlEn = "<p>Article 1 content</p>",
                BodyHtmlAr = "<p>Arabic content</p>",
                TitleAr = "مقال 1",
                Tags = "Marketing, SEO, Growth",
                IsPublished = true
            });

            await _service.CreateAsync(new ArticleInput
            {
                TitleEn = "Article 2",
                BodyHtmlEn = "<p>Article 2 content</p>",
                BodyHtmlAr = "<p>Arabic content</p>",
                TitleAr = "مقال 2",
                Tags = "SEO, Analytics, Growth",
                IsPublished = true
            });

            // Act
            var tags = await _service.GetDistinctTagsAsync();

            // Assert
            tags.Should().Equal("Analytics", "Growth", "Marketing", "SEO");
        }

        [Fact]
        public async Task GetDashboardStatsAsync_AggregatesAllMetricsCorrectly()
        {
            // Arrange
            await _service.CreateAsync(new ArticleInput
            {
                TitleEn = "Published Article",
                BodyHtmlEn = "<p>Published content</p>",
                BodyHtmlAr = "<p>Arabic content</p>",
                TitleAr = "مقال منشور",
                IsPublished = true
            });

            await _service.CreateAsync(new ArticleInput
            {
                TitleEn = "Draft Article",
                BodyHtmlEn = "<p>Draft content</p>",
                BodyHtmlAr = "<p>Arabic content</p>",
                TitleAr = "مقال مسودة",
                IsPublished = false
            });

            _db.BrandProfiles.Add(new BrandProfile { NameEn = "Brand 1", NameAr = "براند 1", Slug = "brand-1" });
            _db.CmsServices.Add(new CmsService { NameEn = "Service 1", NameAr = "خدمة 1", Slug = "srv-1" });
            _db.CmsProjects.Add(new CmsProject { NameEn = "Project 1", NameAr = "مشروع 1", Slug = "prj-1" });
            _db.TeamMembers.Add(new TeamMember { NameEn = "Member 1", NameAr = "عضو 1", TitleEn = "Dev", TitleAr = "مطور", Slug = "mem-1" });
            _db.Testimonials.Add(new Testimonial { CustomerNameEn = "Client 1", CustomerNameAr = "عميل 1", ContentEn = "Great", ContentAr = "رائع" });
            await _db.SaveChangesAsync();

            // Act
            var stats = await _service.GetDashboardStatsAsync();

            // Assert
            stats.TotalArticles.Should().Be(2);
            stats.PublishedArticles.Should().Be(1);
            stats.DraftArticles.Should().Be(1);
            stats.TotalBrands.Should().Be(1);
            stats.TotalServices.Should().Be(1);
            stats.TotalProjects.Should().Be(1);
            stats.TotalTeamMembers.Should().Be(1);
            stats.TotalTestimonials.Should().Be(1);
        }
    }
}
