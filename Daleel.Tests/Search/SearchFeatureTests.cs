using Daleel.BAL.Models;
using Daleel.BAL.Services;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Daleel.Tests.Common;

namespace Daleel.Tests.Search
{
    public class SearchFeatureTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly ArticleService _articleService;
        private readonly CompanyService _companyService;
        private readonly ContactService _contactService;
        private readonly LeadService _leadService;
        private readonly DealService _dealService;
        private readonly TaskService _taskService;
        private readonly CmsServiceService _cmsServiceService;
        private readonly CmsProjectService _cmsProjectService;
        private readonly TeamMemberService _teamMemberService;
        private readonly TestimonialService _testimonialService;

        public SearchFeatureTests()
        {
            _db = TestDbContextFactory.Create();
            _articleService = new ArticleService(_db);
            _companyService = new CompanyService(_db);
            _contactService = new ContactService(_db);
            _leadService = new LeadService(_db, Mock.Of<Microsoft.Extensions.Logging.ILogger<LeadService>>());
            _dealService = new DealService(_db);
            _taskService = new TaskService(_db);
            _cmsServiceService = new CmsServiceService(_db);
            _cmsProjectService = new CmsProjectService(_db);
            _teamMemberService = new TeamMemberService(_db);
            _testimonialService = new TestimonialService(_db);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }

        #region Public Blog / Article Search Tests

        [Fact]
        public async Task ArticleSearch_ByEnglishTitle_ReturnsMatchingArticles()
        {
            // Arrange
            _db.Articles.AddRange(
                new Article { TitleEn = "Modern ERP Cloud Transformation", TitleAr = "التحول الرقمي", IsPublished = true, Slug = "erp-cloud" },
                new Article { TitleEn = "Customer Success Best Practices", TitleAr = "أفضل الممارسات", IsPublished = true, Slug = "cs-practices" }
            );
            await _db.SaveChangesAsync();

            // Act
            var result = await _articleService.SearchAsync(new ArticleQuery { Q = "Cloud", IsPublished = true });

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items[0].TitleEn.Should().Be("Modern ERP Cloud Transformation");
        }

        [Fact]
        public async Task ArticleSearch_ByArabicTitle_ReturnsMatchingArticles()
        {
            // Arrange
            _db.Articles.AddRange(
                new Article { TitleEn = "Vision 2030 Strategies", TitleAr = "استراتيجيات رؤية 2030 للتقنية", IsPublished = true, Slug = "vision-2030" },
                new Article { TitleEn = "General Updates", TitleAr = "تحديثات عامة", IsPublished = true, Slug = "general" }
            );
            await _db.SaveChangesAsync();

            // Act
            var result = await _articleService.SearchAsync(new ArticleQuery { Q = "رؤية 2030", IsPublished = true });

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items[0].TitleAr.Should().Be("استراتيجيات رؤية 2030 للتقنية");
        }

        [Fact]
        public async Task ArticleSearch_BySummary_ReturnsMatchingArticles()
        {
            // Arrange
            _db.Articles.AddRange(
                new Article
                {
                    TitleEn = "Tech Guide",
                    TitleAr = "دليل التقنية",
                    SummaryEn = "Deep dive into artificial intelligence and machine learning pipelines.",
                    IsPublished = true,
                    Slug = "tech-guide"
                },
                new Article
                {
                    TitleEn = "HR Insights",
                    TitleAr = "رؤى الموارد البشرية",
                    SummaryEn = "Recruiting top talent in Saudi Arabia.",
                    IsPublished = true,
                    Slug = "hr-insights"
                }
            );
            await _db.SaveChangesAsync();

            // Act
            var result = await _articleService.SearchAsync(new ArticleQuery { Q = "machine learning", IsPublished = true });

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items[0].TitleEn.Should().Be("Tech Guide");
        }

        [Fact]
        public async Task ArticleSearch_ByCategoryAll_DoesNotFilterOutCategories()
        {
            // Arrange
            _db.Articles.AddRange(
                new Article { TitleEn = "AI Strategy", TitleAr = "الذكاء الاصطناعي", Category = "AI & Analytics", IsPublished = true, Slug = "ai-strat" },
                new Article { TitleEn = "CRM Modernization", TitleAr = "تحديث إدارة العملاء", Category = "CRM Solutions", IsPublished = true, Slug = "crm-mod" }
            );
            await _db.SaveChangesAsync();

            // Act - Passing "All" should return all published articles
            var result = await _articleService.SearchAsync(new ArticleQuery { Category = "All", IsPublished = true });

            // Assert
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task ArticleSearch_BySpecificCategoryAndTag_ReturnsExactMatch()
        {
            // Arrange
            _db.Articles.AddRange(
                new Article { TitleEn = "Art 1", TitleAr = "1", Category = "Tech", Tags = "AI, Cloud, SA", IsPublished = true, Slug = "a1" },
                new Article { TitleEn = "Art 2", TitleAr = "2", Category = "Tech", Tags = "Database, SQL", IsPublished = true, Slug = "a2" },
                new Article { TitleEn = "Art 3", TitleAr = "3", Category = "Finance", Tags = "AI, FinTech", IsPublished = true, Slug = "a3" }
            );
            await _db.SaveChangesAsync();

            // Act - Filter Category: Tech AND Tag: AI
            var result = await _articleService.SearchAsync(new ArticleQuery { Category = "Tech", Tag = "AI", IsPublished = true });

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items[0].Slug.Should().Be("a1");
        }

        [Fact]
        public async Task ArticleSearch_PublicSearch_ExcludesUnpublishedAndFutureScheduled()
        {
            // Arrange
            _db.Articles.AddRange(
                new Article { TitleEn = "Published Article", TitleAr = "مقال منشور", IsPublished = true, Slug = "pub" },
                new Article { TitleEn = "Draft Article", TitleAr = "مسودة", IsPublished = false, Slug = "draft" },
                new Article { TitleEn = "Scheduled Future Article", TitleAr = "مستقبلي", IsPublished = true, ScheduledPublishAt = DateTime.UtcNow.AddDays(5), Slug = "scheduled" }
            );
            await _db.SaveChangesAsync();

            // Act
            var result = await _articleService.SearchAsync(new ArticleQuery { IsPublished = true });

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items[0].Slug.Should().Be("pub");
        }

        #endregion

        #region CRM Back-Office Search Tests

        [Fact]
        public async Task CompanySearch_ByMultipleFields_ReturnsMatchingCompanies()
        {
            // Arrange
            _db.Companies.AddRange(
                new Company { Name = "Advanced Electronics Co", Industry = "Defense Tech", City = "Riyadh", Status = RecordStatus.Active },
                new Company { Name = "Petro Chemical Int", Industry = "Energy", City = "Jubail", Status = RecordStatus.Active },
                new Company { Name = "Retail Hub", Industry = "Retail", City = "Jeddah", Phone = "+966119998888", Status = RecordStatus.Active }
            );
            await _db.SaveChangesAsync();

            // Act - Search by City
            var riyadhResult = await _companyService.SearchAsync(new CompanyQuery { Q = "Riyadh" });
            // Act - Search by Industry
            var energyResult = await _companyService.SearchAsync(new CompanyQuery { Q = "Energy" });
            // Act - Search by Phone
            var phoneResult = await _companyService.SearchAsync(new CompanyQuery { Q = "+966119998888" });

            // Assert
            riyadhResult.TotalCount.Should().Be(1);
            riyadhResult.Items[0].Name.Should().Be("Advanced Electronics Co");

            energyResult.TotalCount.Should().Be(1);
            energyResult.Items[0].Name.Should().Be("Petro Chemical Int");

            phoneResult.TotalCount.Should().Be(1);
            phoneResult.Items[0].Name.Should().Be("Retail Hub");
        }

        [Fact]
        public async Task ContactSearch_ByCompanyOrJobTitle_ReturnsMatches()
        {
            // Arrange
            var company = new Company { Name = "Zamil Group" };
            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            _db.Contacts.AddRange(
                new Contact { FirstName = "Tariq", LastName = "Al-Zamil", JobTitle = "Managing Director", CompanyId = company.Id, Status = RecordStatus.Active },
                new Contact { FirstName = "Sarah", LastName = "Connor", JobTitle = "Security Engineer", Status = RecordStatus.Active }
            );
            await _db.SaveChangesAsync();

            // Act - Search by Company Name
            var companyMatch = await _contactService.SearchAsync(new ContactQuery { Q = "Zamil" });
            // Act - Search by Job Title
            var titleMatch = await _contactService.SearchAsync(new ContactQuery { Q = "Security Engineer" });

            // Assert
            companyMatch.TotalCount.Should().Be(1);
            companyMatch.Items[0].FirstName.Should().Be("Tariq");

            titleMatch.TotalCount.Should().Be(1);
            titleMatch.Items[0].FirstName.Should().Be("Sarah");
        }

        [Fact]
        public async Task LeadSearch_ByKeyword_SearchesAllLeadFields()
        {
            // Arrange
            _db.Leads.AddRange(
                new Lead { FirstName = "Fahad", LastName = "Al-Otaibi", CompanyName = "Otaibi Contracting", Email = "fahad@otaibi.sa", Status = LeadStatus.New },
                new Lead { FirstName = "John", LastName = "Smith", CompanyName = "Acme", Email = "john@acme.com", JobTitle = "Procurement VP", Status = LeadStatus.Qualified }
            );
            await _db.SaveChangesAsync();

            // Act
            var nameMatch = await _leadService.SearchAsync(new LeadQuery { Q = "Otaibi" });
            var jobMatch = await _leadService.SearchAsync(new LeadQuery { Q = "Procurement" });

            // Assert
            nameMatch.TotalCount.Should().Be(1);
            nameMatch.Items[0].FirstName.Should().Be("Fahad");

            jobMatch.TotalCount.Should().Be(1);
            jobMatch.Items[0].FirstName.Should().Be("John");
        }

        [Fact]
        public async Task DealSearch_ByDealNameOrCompany_ReturnsMatches()
        {
            // Arrange
            var comp = new Company { Name = "National Grid SA" };
            _db.Companies.Add(comp);
            await _db.SaveChangesAsync();

            _db.Deals.AddRange(
                new Deal { Name = "Smart Metering System 2026", CompanyId = comp.Id, Value = 1200000m, Stage = DealStage.Negotiation },
                new Deal { Name = "Cloud Migration Phase 1", Value = 300000m, Stage = DealStage.New }
            );
            await _db.SaveChangesAsync();

            // Act
            var dealNameSearch = await _dealService.SearchAsync(new DealQuery { Q = "Smart Metering" });
            var companySearch = await _dealService.SearchAsync(new DealQuery { Q = "National Grid" });

            // Assert
            dealNameSearch.TotalCount.Should().Be(1);
            dealNameSearch.Items[0].Name.Should().Be("Smart Metering System 2026");

            companySearch.TotalCount.Should().Be(1);
            companySearch.Items[0].Name.Should().Be("Smart Metering System 2026");
        }

        [Fact]
        public async Task TaskSearch_ByTitleOrDescription_ReturnsMatches()
        {
            // Arrange
            _db.CrmTasks.AddRange(
                new CrmTask { Title = "Deliver Security Audit", Description = "Prepare ISO 27001 readiness report", Status = CrmTaskStatus.Open },
                new CrmTask { Title = "Update License Pricing", Description = "Revise tiered discount structure", Status = CrmTaskStatus.Open }
            );
            await _db.SaveChangesAsync();

            // Act
            var searchDesc = await _taskService.SearchAsync(new TaskQuery { Q = "ISO 27001" });

            // Assert
            searchDesc.TotalCount.Should().Be(1);
            searchDesc.Items[0].Title.Should().Be("Deliver Security Audit");
        }

        #endregion

        #region CMS Back-Office Search Tests

        [Fact]
        public async Task CmsServiceSearch_ByBilingualNameOrShortDescription_ReturnsMatches()
        {
            // Arrange
            var brand = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            _db.CmsServices.AddRange(
                new CmsService
                {
                    BrandId = brand.Id,
                    NameEn = "AI Process Automation",
                    NameAr = "أتمتة العمليات بالذكاء الاصطناعي",
                    ShortDescEn = "Automate back-office routines with intelligent bots.",
                    Slug = "ai-automation",
                    IsPublished = true
                },
                new CmsService
                {
                    BrandId = brand.Id,
                    NameEn = "Cloud ERP Infrastructure",
                    NameAr = "البنية التحتية السحابية",
                    ShortDescEn = "Scalable enterprise databases and microservices.",
                    Slug = "cloud-erp",
                    IsPublished = true
                }
            );
            await _db.SaveChangesAsync();

            // Act - Arabic Search
            var arResult = await _cmsServiceService.SearchAsync(new CmsServiceQuery { Q = "أتمتة العمليات" });
            // Act - English Description Search
            var enDescResult = await _cmsServiceService.SearchAsync(new CmsServiceQuery { Q = "intelligent bots" });

            // Assert
            arResult.TotalCount.Should().Be(1);
            arResult.Items[0].Slug.Should().Be("ai-automation");

            enDescResult.TotalCount.Should().Be(1);
            enDescResult.Items[0].Slug.Should().Be("ai-automation");
        }

        [Fact]
        public async Task CmsProjectSearch_ByClientOrDescription_ReturnsMatches()
        {
            // Arrange
            var brand = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            _db.CmsProjects.AddRange(
                new CmsProject
                {
                    BrandId = brand.Id,
                    NameEn = "Ministry Data Portal",
                    NameAr = "بوابة البيانات الوزارية",
                    ClientNameEn = "Ministry of Communications",
                    ShortDescEn = "Government Tech",
                    Slug = "ministry-portal",
                    IsPublished = true
                },
                new CmsProject
                {
                    BrandId = brand.Id,
                    NameEn = "FinTech Payment Gateway",
                    NameAr = "بوابة الدفع المالي",
                    ClientNameEn = "PaySaudi",
                    ShortDescEn = "FinTech",
                    Slug = "pay-saudi",
                    IsPublished = true
                }
            );
            await _db.SaveChangesAsync();

            // Act
            var clientResult = await _cmsProjectService.SearchAsync(new CmsProjectQuery { Q = "Ministry of Communications" });
            var nameResult = await _cmsProjectService.SearchAsync(new CmsProjectQuery { Q = "FinTech Payment" });

            // Assert
            clientResult.TotalCount.Should().Be(1);
            clientResult.Items[0].Slug.Should().Be("ministry-portal");

            nameResult.TotalCount.Should().Be(1);
            nameResult.Items[0].Slug.Should().Be("pay-saudi");
        }

        [Fact]
        public async Task TeamMemberSearch_ByTitleOrName_ReturnsMatches()
        {
            // Arrange
            var brand = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            _db.TeamMembers.AddRange(
                new TeamMember
                {
                    BrandId = brand.Id,
                    NameEn = "Dr. Khalid Mansoor",
                    NameAr = "د. خالد منصور",
                    TitleEn = "Chief AI Architect",
                    TitleAr = "كبير مهندسي الذكاء الاصطناعي",
                    BioEn = "Artificial Intelligence expert",
                    IsPublished = true
                },
                new TeamMember
                {
                    BrandId = brand.Id,
                    NameEn = "Reem Al-Ghamdi",
                    NameAr = "ريم الغامدي",
                    TitleEn = "Lead UI/UX Designer",
                    TitleAr = "رئيسة تصميم تجربة المستخدم",
                    BioEn = "Product Design lead",
                    IsPublished = true
                }
            );
            await _db.SaveChangesAsync();

            // Act
            var titleResult = await _teamMemberService.SearchAsync(new TeamMemberQuery { Q = "AI Architect" });
            var roleResult = await _teamMemberService.SearchAsync(new TeamMemberQuery { Q = "UI/UX" });

            // Assert
            titleResult.TotalCount.Should().Be(1);
            titleResult.Items[0].NameEn.Should().Be("Dr. Khalid Mansoor");

            roleResult.TotalCount.Should().Be(1);
            roleResult.Items[0].NameEn.Should().Be("Reem Al-Ghamdi");
        }

        [Fact]
        public async Task TestimonialSearch_ByCustomerNameOrCompany_ReturnsMatches()
        {
            // Arrange
            var brand = new BrandProfile { NameEn = "Daleel", NameAr = "دليل", Slug = "daleel" };
            _db.BrandProfiles.Add(brand);
            await _db.SaveChangesAsync();

            _db.Testimonials.AddRange(
                new Testimonial
                {
                    BrandId = brand.Id,
                    CustomerNameEn = "Sultan Al-Harbi",
                    CustomerNameAr = "سلطان الحربي",
                    CompanyNameEn = "Etihad Logistics",
                    ContentEn = "Transformative partnership that drove 40% efficiency.",
                    ContentAr = "شراكة متميزة رفعت الكفاءة.",
                    Rating = 5,
                    IsPublished = true
                },
                new Testimonial
                {
                    BrandId = brand.Id,
                    CustomerNameEn = "Fatima Al-Sayed",
                    CustomerNameAr = "فاطمة السيد",
                    CompanyNameEn = "Healthcare First",
                    ContentEn = "Top-tier CRM implementation.",
                    ContentAr = "تطبيق متميز لنظام إدارة علاقات العملاء.",
                    Rating = 5,
                    IsPublished = true
                }
            );
            await _db.SaveChangesAsync();

            // Act
            var compResult = await _testimonialService.SearchAsync(new TestimonialQuery { Q = "Etihad Logistics" });
            var nameResult = await _testimonialService.SearchAsync(new TestimonialQuery { Q = "فاطمة السيد" });

            // Assert
            compResult.TotalCount.Should().Be(1);
            compResult.Items[0].CustomerNameEn.Should().Be("Sultan Al-Harbi");

            nameResult.TotalCount.Should().Be(1);
            nameResult.Items[0].CustomerNameAr.Should().Be("فاطمة السيد");
        }

        #endregion
    }
}
