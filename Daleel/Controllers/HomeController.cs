using System.Diagnostics;
using System.Globalization;
using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Entities;
using Daleel.Models;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILeadService _leads;
        private readonly IArticleService _articles;
        private readonly IPageSectionService _pageSections;
        private readonly IBrandProfileService _brands;
        private readonly ICmsServiceService _services;
        private readonly ICmsProjectService _projects;
        private readonly ITeamMemberService _team;
        private readonly ITestimonialService _testimonials;

        public HomeController(
            ILeadService leads,
            IArticleService articles,
            IPageSectionService pageSections,
            IBrandProfileService brands,
            ICmsServiceService services,
            ICmsProjectService projects,
            ITeamMemberService team,
            ITestimonialService testimonials)
        {
            _leads = leads;
            _articles = articles;
            _pageSections = pageSections;
            _brands = brands;
            _services = services;
            _projects = projects;
            _team = team;
            _testimonials = testimonials;
        }

        public async Task<IActionResult> Index()
        {
            var brandSlug = GetCurrentBrandSlug();
            var culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            var content = await _pageSections.GetPageContentMapAsync("home", culture);
            var brand = await _brands.GetBySlugAsync(brandSlug);
            var services = await _services.GetByBrandSlugAsync(brandSlug, onlyPublished: true);
            var projects = await _projects.GetByBrandSlugAsync(brandSlug, onlyPublished: true);
            var team = await _team.GetByBrandSlugAsync(brandSlug, onlyPublished: true);
            var testimonials = await _testimonials.GetByBrandSlugAsync(brandSlug, onlyPublished: true);
            var recentArticles = await _articles.GetRecentPublishedAsync(3);

            var model = new HomePageViewModel
            {
                Brand = brand,
                Services = services,
                Projects = projects,
                TeamMembers = team,
                Testimonials = testimonials,
                RecentArticles = recentArticles
            };

            ViewData["PageContent"] = content;
            return View(model);
        }

        [HttpGet]
        [Route("platforms")]
        [Route("home/platforms")]
        public async Task<IActionResult> Platforms()
        {
            var brandSlug = GetCurrentBrandSlug();
            var culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            var content = await _pageSections.GetPageContentMapAsync("platforms", culture);
            var brand = await _brands.GetBySlugAsync(brandSlug);
            var services = await _services.GetByBrandSlugAsync(brandSlug, onlyPublished: true);
            var projects = await _projects.GetByBrandSlugAsync(brandSlug, onlyPublished: true);

            var model = new PlatformsPageViewModel
            {
                Brand = brand,
                Services = services,
                Projects = projects
            };

            ViewData["PageContent"] = content;
            return View(model);
        }

        [HttpGet]
        [Route("shop")]
        [Route("home/shop")]
        public async Task<IActionResult> Shop()
        {
            var culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            ViewData["PageContent"] = await _pageSections.GetPageContentMapAsync("shop", culture);
            return View();
        }

        [HttpGet]
        [Route("trust")]
        [Route("home/trust")]
        public async Task<IActionResult> Trust()
        {
            var culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var content = await _pageSections.GetPageContentMapAsync("trust", culture);
            ViewData["PageContent"] = content;
            return View();
        }

        [HttpGet]
        [Route("about")]
        [Route("home/about")]
        public async Task<IActionResult> About()
        {
            var brandSlug = GetCurrentBrandSlug();
            var culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            var content = await _pageSections.GetPageContentMapAsync("about", culture);
            var brand = await _brands.GetBySlugAsync(brandSlug);
            var team = await _team.GetByBrandSlugAsync(brandSlug, onlyPublished: true);
            var testimonials = await _testimonials.GetByBrandSlugAsync(brandSlug, onlyPublished: true);

            var model = new AboutPageViewModel
            {
                Brand = brand,
                TeamMembers = team,
                Testimonials = testimonials
            };

            ViewData["PageContent"] = content;
            return View(model);
        }

        [HttpGet]
        [Route("blog")]
        [Route("home/blog")]
        public async Task<IActionResult> Blog(string? q, string? category, string? tag, int page = 1)
        {
            var result = await _articles.SearchAsync(new ArticleQuery
            {
                Q = q,
                Category = category,
                Tag = tag,
                IsPublished = true,
                Page = page,
                PageSize = 9
            });

            var dbCategories = await _articles.GetDistinctCategoriesAsync();
            var categories = new List<string> { "All" };
            if (dbCategories.Count > 0)
            {
                categories.AddRange(dbCategories);
            }
            else
            {
                categories.AddRange(new[] { "AI & Analytics", "Enterprise Tech", "CRM Solutions", "Digital Transformation", "Industry News" });
            }

            var dbTags = await _articles.GetDistinctTagsAsync();

            // Blog landing hero copy only — the article list itself comes from _articles above.
            var culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            ViewData["PageContent"] = await _pageSections.GetPageContentMapAsync("blog", culture);

            var model = new BlogIndexViewModel
            {
                Articles = result,
                SelectedCategory = category,
                SelectedTag = tag,
                SearchTerm = q,
                Categories = categories,
                Tags = dbTags
            };

            return View(model);
        }

        [HttpGet]
        [Route("article/{slug?}")]
        [Route("home/article/{slug?}")]
        public async Task<IActionResult> Article(string? slug)
        {
            if (!string.IsNullOrWhiteSpace(slug))
            {
                var article = await _articles.GetBySlugAsync(slug, onlyPublished: true);
                if (article is null)
                {
                    // Check if authenticated user can view draft preview
                    if (User.Identity?.IsAuthenticated == true)
                    {
                        article = await _articles.GetBySlugAsync(slug, onlyPublished: false);
                    }
                }

                if (article is null)
                {
                    return NotFound();
                }

                var related = await _articles.GetRecentPublishedAsync(3);
                ViewData["RelatedArticles"] = related.Where(r => r.Id != article.Id).Take(2).ToList();

                return View(article);
            }

            // Fallback: If no slug passed, load the latest published article or a default view
            var latest = (await _articles.GetRecentPublishedAsync(1)).FirstOrDefault();
            if (latest != null)
            {
                var fullLatest = await _articles.GetByIdAsync(latest.Id);
                if (fullLatest != null)
                {
                    return View(fullLatest);
                }
            }

            return View((ArticleDetailDto?)null);
        }

        [HttpGet]
        [Route("contact")]
        [Route("home/contact")]
        public async Task<IActionResult> Contact()
        {
            var brandSlug = GetCurrentBrandSlug();
            var brand = await _brands.GetBySlugAsync(brandSlug);
            var model = new ContactPageViewModel
            {
                Brand = brand,
                Form = new ContactInquiryViewModel()
            };

            ViewData["PageContent"] = await LoadContactContentAsync();
            return View(model);
        }

        // Headings/copy for the contact page. The enquiry form keeps its own view model,
        // validation and lead capture — page sections govern the surrounding copy only.
        private Task<IReadOnlyDictionary<string, string>> LoadContactContentAsync() =>
            _pageSections.GetPageContentMapAsync("contact", CultureInfo.CurrentCulture.TwoLetterISOLanguageName);

        [HttpPost]
        [Route("contact")]
        [Route("home/contact")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactPageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var brandSlug = GetCurrentBrandSlug();
                model.Brand = await _brands.GetBySlugAsync(brandSlug);
                ViewData["PageContent"] = await LoadContactContentAsync();
                return View(model);
            }

            await _leads.CaptureAsync(new LeadCaptureInput
            {
                FirstName = model.Form.FirstName,
                LastName = model.Form.LastName,
                Email = model.Form.Email,
                Source = LeadSource.ContactForm,
                Notes = $"Subject: {model.Form.Subject}\n\n{model.Form.Message}"
            });

            // PRG pattern – prevents duplicate submissions on browser refresh
            TempData["ContactSuccess"] = true;
            return RedirectToAction(nameof(Contact));
        }

        [HttpGet]
        [Route("partnerregistration")]
        [Route("home/partnerregistration")]
        public IActionResult PartnerRegistration()
        {
            return View(new PartnerRegistrationViewModel());
        }

        [HttpPost]
        [Route("partnerregistration")]
        [Route("home/partnerregistration")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PartnerRegistration(PartnerRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _leads.CaptureAsync(new LeadCaptureInput
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                CompanyName = model.CompanyName,
                Source = LeadSource.PartnerInquiry,
                Notes = $"Partnership type: {model.PartnershipType}\n\n{model.Message}"
            });

            TempData["PartnerSuccess"] = true;
            return RedirectToAction(nameof(PartnerRegistration));
        }

        [HttpGet]
        [Route("soon")]
        [Route("home/soon")]
        public async Task<IActionResult> Soon()
        {
            var culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            ViewData["PageContent"] = await _pageSections.GetPageContentMapAsync("soon", culture);
            return View();
        }

        [HttpGet]
        [Route("scheduledemo")]
        [Route("home/scheduledemo")]
        public IActionResult ScheduleDemo()
        {
            return View(new ScheduleDemoViewModel());
        }

        [HttpPost]
        [Route("scheduledemo")]
        [Route("home/scheduledemo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleDemo(ScheduleDemoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _leads.CaptureAsync(new LeadCaptureInput
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.WorkEmail,
                CompanyName = model.Company,
                JobTitle = model.JobTitle,
                Source = LeadSource.ScheduleDemo,
                Notes = "Requested a platform demo."
            });

            TempData["DemoSuccess"] = true;
            return RedirectToAction(nameof(ScheduleDemo));
        }

        public async Task<IActionResult> Pricing()
        {
            var culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var content = await _pageSections.GetPageContentMapAsync("pricing", culture);
            ViewData["PageContent"] = content;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        private string GetCurrentBrandSlug()
        {
            var brandParam = Request.Query["brand"].ToString();
            if (!string.IsNullOrWhiteSpace(brandParam))
            {
                return brandParam.Trim().ToLowerInvariant();
            }
            return "daleel";
        }
    }
}
