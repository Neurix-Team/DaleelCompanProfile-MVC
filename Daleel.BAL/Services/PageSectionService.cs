using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Daleel.BAL.Services
{
    public class PageSectionService : IPageSectionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private const string CachePrefix = "PageSection_";

        public PageSectionService(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<IReadOnlyList<PageSectionDto>> GetSectionsByPageAsync(string pageKey)
        {
            if (string.IsNullOrWhiteSpace(pageKey)) return Array.Empty<PageSectionDto>();

            var key = pageKey.ToLowerInvariant().Trim();
            return await _db.PageSections
                .AsNoTracking()
                .Where(p => p.PageKey == key)
                .OrderBy(p => p.SectionKey)
                .Select(p => ToDto(p))
                .ToListAsync();
        }

        public async Task<IReadOnlyDictionary<string, string>> GetPageContentMapAsync(string pageKey, string culture = "en")
        {
            if (string.IsNullOrWhiteSpace(pageKey)) return new Dictionary<string, string>();

            var key = pageKey.ToLowerInvariant().Trim();
            var isAr = culture.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
            var cacheKey = $"{CachePrefix}{key}_{(isAr ? "ar" : "en")}";

            if (_cache.TryGetValue(cacheKey, out IReadOnlyDictionary<string, string>? cached) && cached != null)
            {
                return cached;
            }

            var sections = await _db.PageSections
                .AsNoTracking()
                .Where(p => p.PageKey == key)
                .ToListAsync();

            var map = sections.ToDictionary(
                s => s.SectionKey,
                s => isAr ? (string.IsNullOrEmpty(s.ValueAr) ? s.ValueEn : s.ValueAr)
                          : (string.IsNullOrEmpty(s.ValueEn) ? s.ValueAr : s.ValueEn),
                StringComparer.OrdinalIgnoreCase
            );

            _cache.Set(cacheKey, map, TimeSpan.FromMinutes(10));
            return map;
        }

        public async Task<PageSectionDto?> GetAsync(string pageKey, string sectionKey)
        {
            var pKey = pageKey.ToLowerInvariant().Trim();
            var sKey = sectionKey.ToLowerInvariant().Trim();

            var entity = await _db.PageSections
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PageKey == pKey && p.SectionKey == sKey);

            return entity is null ? null : ToDto(entity);
        }

        public async Task<IReadOnlyList<string>> GetDistinctPagesAsync()
        {
            var pages = await _db.PageSections
                .AsNoTracking()
                .Select(p => p.PageKey)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            if (pages.Count == 0)
            {
                return new[] { "home", "about", "platforms", "trust", "shop", "blog", "contact", "soon" };
            }

            return pages;
        }

        public async Task<ServiceResult> UpsertAsync(PageSectionInput input)
        {
            if (string.IsNullOrWhiteSpace(input.PageKey) || string.IsNullOrWhiteSpace(input.SectionKey))
            {
                return ServiceResult.Invalid("PageKey", "Page and Section keys are required.");
            }

            var pKey = input.PageKey.ToLowerInvariant().Trim();
            var sKey = input.SectionKey.ToLowerInvariant().Trim();

            var existing = await _db.PageSections
                .FirstOrDefaultAsync(p => p.PageKey == pKey && p.SectionKey == sKey);

            if (existing != null)
            {
                existing.ValueEn = input.ValueEn ?? string.Empty;
                existing.ValueAr = input.ValueAr ?? string.Empty;
                existing.DataType = string.IsNullOrWhiteSpace(input.DataType) ? "Text" : input.DataType.Trim();
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.PageSections.Add(new PageSection
                {
                    PageKey = pKey,
                    SectionKey = sKey,
                    ValueEn = input.ValueEn ?? string.Empty,
                    ValueAr = input.ValueAr ?? string.Empty,
                    DataType = string.IsNullOrWhiteSpace(input.DataType) ? "Text" : input.DataType.Trim(),
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            InvalidatePageCache(pKey);

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> BulkUpsertAsync(string pageKey, IEnumerable<PageSectionInput> inputs)
        {
            if (string.IsNullOrWhiteSpace(pageKey))
            {
                return ServiceResult.Invalid("PageKey", "Page key is required.");
            }

            var pKey = pageKey.ToLowerInvariant().Trim();
            var existingSections = await _db.PageSections
                .Where(p => p.PageKey == pKey)
                .ToListAsync();

            var existingMap = existingSections.ToDictionary(p => p.SectionKey.ToLowerInvariant());

            foreach (var input in inputs)
            {
                if (string.IsNullOrWhiteSpace(input.SectionKey)) continue;

                var sKey = input.SectionKey.ToLowerInvariant().Trim();
                if (existingMap.TryGetValue(sKey, out var existing))
                {
                    existing.ValueEn = input.ValueEn ?? string.Empty;
                    existing.ValueAr = input.ValueAr ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(input.DataType))
                    {
                        existing.DataType = input.DataType.Trim();
                    }
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _db.PageSections.Add(new PageSection
                    {
                        PageKey = pKey,
                        SectionKey = sKey,
                        ValueEn = input.ValueEn ?? string.Empty,
                        ValueAr = input.ValueAr ?? string.Empty,
                        DataType = string.IsNullOrWhiteSpace(input.DataType) ? "Text" : input.DataType.Trim(),
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync();
            InvalidatePageCache(pKey);

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> DeleteSectionAsync(string pageKey, string sectionKey)
        {
            if (string.IsNullOrWhiteSpace(pageKey) || string.IsNullOrWhiteSpace(sectionKey))
            {
                return ServiceResult.Invalid("SectionKey", "Page and Section keys are required.");
            }

            var pKey = pageKey.ToLowerInvariant().Trim();
            var sKey = sectionKey.ToLowerInvariant().Trim();

            var entity = await _db.PageSections
                .FirstOrDefaultAsync(p => p.PageKey == pKey && p.SectionKey == sKey);

            if (entity is null)
            {
                return ServiceResult.Missing();
            }

            _db.PageSections.Remove(entity);
            await _db.SaveChangesAsync();
            InvalidatePageCache(pKey);

            return ServiceResult.Success();
        }

        /// <summary>
        /// Seeds defaults for any page key that has no rows yet, leaving every existing row
        /// untouched. Deliberately per-page rather than "only when the whole table is empty":
        /// an installation that already holds the original four pages must still receive new
        /// ones, and an admin's edits to those four must never be overwritten.
        /// </summary>
        public async Task SeedMissingDefaultSectionsAsync()
        {
            var existingPageKeys = await _db.PageSections
                .AsNoTracking()
                .Select(p => p.PageKey)
                .Distinct()
                .ToListAsync();

            var missing = DefaultSections()
                .Where(s => !existingPageKeys.Contains(s.PageKey))
                .ToList();

            if (missing.Count == 0) return;

            _db.PageSections.AddRange(missing);
            await _db.SaveChangesAsync();

            foreach (var pageKey in missing.Select(s => s.PageKey).Distinct())
            {
                InvalidatePageCache(pageKey);
            }
        }

        public async Task<int> SyncMissingSectionKeysAsync()
        {
            var existingKeys = await _db.PageSections
                .AsNoTracking()
                .Select(p => new { p.PageKey, p.SectionKey })
                .ToListAsync();

            var existingSet = existingKeys.Select(k => (k.PageKey, k.SectionKey)).ToHashSet();

            var missing = DefaultSections()
                .Where(s => !existingSet.Contains((s.PageKey, s.SectionKey)))
                .ToList();

            if (missing.Count == 0) return 0;

            _db.PageSections.AddRange(missing);
            await _db.SaveChangesAsync();

            foreach (var pageKey in missing.Select(s => s.PageKey).Distinct())
            {
                InvalidatePageCache(pageKey);
            }

            return missing.Count;
        }

        /// <summary>
        /// The original four pages were seeded with placeholder copy describing a hero that the
        /// markup never rendered — none of those strings appeared anywhere on the site. Now that
        /// the views read these sections, leaving those values in place would push unrelated text
        /// onto live pages. This rewrites such a row to its aligned default, but ONLY while it
        /// still holds the exact original placeholder, so anything an admin has edited is left
        /// alone. Idempotent: once rewritten, the match no longer applies.
        /// </summary>
        public async Task<int> AlignLegacyPlaceholderSectionsAsync()
        {
            var aligned = DefaultSections()
                .Where(d => LegacyPlaceholders.ContainsKey((d.PageKey, d.SectionKey)))
                .ToDictionary(d => (d.PageKey, d.SectionKey));

            var rows = await _db.PageSections
                .Where(p => p.PageKey == "home" || p.PageKey == "about"
                         || p.PageKey == "trust" || p.PageKey == "platforms")
                .ToListAsync();

            var changed = 0;
            foreach (var row in rows)
            {
                var id = (row.PageKey, row.SectionKey);
                if (!LegacyPlaceholders.TryGetValue(id, out var placeholderEn)) continue;
                if (!string.Equals(row.ValueEn, placeholderEn, StringComparison.Ordinal)) continue;
                if (!aligned.TryGetValue(id, out var target)) continue;

                row.ValueEn = target.ValueEn;
                row.ValueAr = target.ValueAr;
                row.UpdatedAt = DateTime.UtcNow;
                changed++;
            }

            if (changed > 0)
            {
                await _db.SaveChangesAsync();
                foreach (var pageKey in rows.Select(r => r.PageKey).Distinct())
                {
                    InvalidatePageCache(pageKey);
                }
            }

            return changed;
        }

        /// <summary>English values the original seeder wrote, keyed by (page, section).</summary>
        private static readonly Dictionary<(string, string), string> LegacyPlaceholders = new()
        {
            [("home", "hero_badge")] = "AI-Powered Enterprise Intelligence",
            [("home", "hero_title")] = "Accelerate Your Business with Daleel Smart Platforms",
            [("home", "hero_subtitle")] = "Next-generation B2B operating system tailored for Saudi enterprises and high-growth businesses.",
            [("home", "hero_cta_primary")] = "Get Started",
            [("home", "hero_cta_secondary")] = "Schedule Demo",
            [("about", "about_title")] = "Empowering Modern Businesses",
            [("about", "about_desc")] = "Daleel is a technology powerhouse providing end-to-end digital solutions, CRM platforms, and artificial intelligence integration for Middle Eastern markets.",
            [("trust", "trust_title")] = "Enterprise-Grade Security & Compliance",
            [("trust", "trust_desc")] = "Your data is hosted in Tier-4 Saudi data centers adhering strictly to NCA standards and national regulatory requirements.",
            [("platforms", "platforms_title")] = "Our Integrated Platforms Ecosystem",
            [("platforms", "platforms_desc")] = "Discover how Daleel's unified platform suite drives efficiency and intelligent business workflows."
        };

        private static List<PageSection> DefaultSections()
        {
            return new List<PageSection>
            {
                // Home page hero. Blank on purpose: these slots render the Brand Profile's
                // tagline/description today, which varies per brand, so there is no single
                // static value that would leave the page unchanged. Blank means "inherit the
                // brand/built-in copy"; typing a value here overrides it for this page.
                new() { PageKey = "home", SectionKey = "hero_badge", ValueEn = "", ValueAr = "", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "hero_title", ValueEn = "", ValueAr = "", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "hero_title_highlight", ValueEn = "Local Precision", ValueAr = "الدقة المحلية", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "hero_subtitle", ValueEn = "", ValueAr = "", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "hero_cta_primary", ValueEn = "", ValueAr = "", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "hero_cta_secondary", ValueEn = "", ValueAr = "", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "hero_scroll_text", ValueEn = "Scroll to explore", ValueAr = "مرر للاستكشاف", DataType = "Text" },

                // Home page story slides 1-4
                new() { PageKey = "home", SectionKey = "story_slide1_badge", ValueEn = "Data Strategy", ValueAr = "ذكاء البيانات", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "story_slide1_title", ValueEn = "Verified Information. Clearer Perspective.", ValueAr = "معلومات موثقة. رؤية أوضح.", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "story_slide1_title_sub", ValueEn = "Smarter Decisions.", ValueAr = "قرارات أذكى", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "story_slide1_desc", ValueEn = "Trusted data, intelligent analysis, and continuous global coverage — all within a unified platform designed to help individuals and organizations understand the world and make decisions driven by knowledge, not noise.", ValueAr = "بيانات موثوقة، وتحليلات ذكية، وتغطية عالمية مستمرة — ضمن منصة موحدة تساعدك على فهم العالم واتخاذ قرارات مبنية على المعرفة، لا الضوضاء.", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "story_slide1_stat1", ValueEn = "Global", ValueAr = "عالمي", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "story_slide1_stat2", ValueEn = "Always-on", ValueAr = "متاح دائماً", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "story_slide1_stat3", ValueEn = "Verified", ValueAr = "مُوثق", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "story_slide1_image", ValueEn = "/assets/images/globe_network.png", ValueAr = "/assets/images/globe_network.png", DataType = "ImagePath" },

                new() { PageKey = "home", SectionKey = "story_slide2_badge", ValueEn = "Analytical Engine", ValueAr = "محرك الدمج العصبي", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "story_slide2_title", ValueEn = "Analysis That Sees", ValueAr = "الذكاء الاصطناعي يرى", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "story_slide2_title_sub", ValueEn = "What You Can't.", ValueAr = "ما لا يمكنك رؤيته.", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "story_slide2_desc", ValueEn = "Our proprietary Neural Fusion Engine cross-correlates geopolitical events with trade flows, sentiment shifts, and macro indicators — uncovering hidden patterns invisible to the human eye.", ValueAr = "تحليل ذكي لقرارات أفضل. أدوات الذكاء الاصطناعي والتحليل الذكي المتقدمة لدينا تعالج البيانات الموثقة عبر جميع مجالات الحياة - مما يتيح لك الوصول إلى قرارات مثلى ومدعومة علميا لحياتك المهنية والشخصية.", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "story_slide2_image", ValueEn = "/assets/images/ai_neural.png", ValueAr = "/assets/images/ai_neural.png", DataType = "ImagePath" },

                new() { PageKey = "home", SectionKey = "story_slide3_badge", ValueEn = "Strategic Command", ValueAr = "القيادة الاستراتيجية", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "story_slide3_title", ValueEn = "Clarity From", ValueAr = "الوضوح من", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "story_slide3_title_sub", ValueEn = "Complexity.", ValueAr = "التعقيد.", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "story_slide3_desc", ValueEn = "From Information to Informed Decisions. Daleel follows a rigorous two-step process: first, gathering and verifying information from a comprehensive, centralized repository, then applying smart analytical tools to help you make the best decisions in every domain of life.", ValueAr = "من المعلومات إلى القرارات المستنيرة. يتبع دليل عملية دقيقة من خطوتين: أولا جمع المعلومات والتحقق منها من مستودع مركزي شامل، ثم تطبيق أدوات التحليل الذكي لمساعدتك على اتخاذ أفضل القرارات في كل مجال من مجالات الحياة.", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "story_slide3_image", ValueEn = "/assets/images/data_dashboard.png", ValueAr = "/assets/images/data_dashboard.png", DataType = "ImagePath" },

                new() { PageKey = "home", SectionKey = "story_slide4_title", ValueEn = "Ready to Navigate With Clarity?", ValueAr = "هل أنت مستعد للإبحار بوضوح؟", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "story_slide4_desc", ValueEn = "Join a global community that trusts Daleel for reliable information, deep economic insight, and meaningful media — within a modern digital ecosystem built on human values, transparency, and ethical responsibility", ValueAr = "انضم إلى مجتمع عالمي يثق بدليل للوصول إلى معلومات موثوقة، ورؤى اقتصادية عميقة، وإعلام هادف — ضمن منظومة رقمية حديثة قائمة على القيم الإنسانية، والشفافية، والمسؤولية الأخلاقية", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "story_slide4_cta", ValueEn = "Explore Platforms", ValueAr = "اكتشف المنصات", DataType = "Text" },

                // Vision & Strategic Positioning (#vision)
                new() { PageKey = "home", SectionKey = "vision_badge", ValueEn = "Enterprise Strategy", ValueAr = "ذكاء بمستوى المؤسسات", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "vision_title", ValueEn = "Precision Strategy", ValueAr = "استراتيجية دقيقة", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "vision_title_highlight", ValueEn = "for the Global Market.", ValueAr = "للسوق العالمي.", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "vision_desc", ValueEn = "Daleel Global Vision provides the navigational clarity required to manage complex international landscapes through data-driven precision.", ValueAr = "يوفر دليل رؤية واضحة مطلوبة لإدارة المشاهد الدولية المعقدة من خلال الدقة المدفوعة بالبيانات.", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "vision_cta", ValueEn = "Explore Platforms", ValueAr = "اكتشف المنصات", DataType = "Text" },

                new() { PageKey = "home", SectionKey = "vision_card1_title", ValueEn = "100% Verified", ValueAr = "١٠٠٪ معلومات", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "vision_card1_subtitle", ValueEn = "INFORMATION", ValueAr = "موثقة", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "vision_card1_desc", ValueEn = "Every data point is documented and thoroughly verified before it reaches you.", ValueAr = "جميع المعلومات موثقة ومتحقق منها بدقة قبل إتاحتها لضمان ثقتك التامة.", DataType = "Text" },

                new() { PageKey = "home", SectionKey = "vision_card2_title", ValueEn = "All Life", ValueAr = "جميع قطاعات", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "vision_card2_subtitle", ValueEn = "SECTORS", ValueAr = "الحياة", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "vision_card2_desc", ValueEn = "Comprehensive coverage across medical, scientific, professional, and commercial domains.", ValueAr = "تغطية شاملة للمجالات الطبية، والعلمية، والمهنية، والتجارية.", DataType = "Text" },

                new() { PageKey = "home", SectionKey = "vision_card3_title", ValueEn = "AI-Powered", ValueAr = "تحليل", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "vision_card3_subtitle", ValueEn = "ANALYSIS", ValueAr = "بالذكاء الاصطناعي", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "vision_card3_desc", ValueEn = "Advanced tools process complex data to help you make scientifically backed decisions.", ValueAr = "أدوات متقدمة تعالج البيانات لمساعدتك في اتخاذ قرارات مثلى ومدعومة علمياً.", DataType = "Text" },

                new() { PageKey = "home", SectionKey = "vision_card4_title", ValueEn = "Continuous", ValueAr = "دعم", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "vision_card4_subtitle", ValueEn = "SUPPORT", ValueAr = "مستمر", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "vision_card4_desc", ValueEn = "Ongoing, reliable assistance to help you navigate modern life with clarity and purpose.", ValueAr = "مساندة دائمة وموثوقة لمساعدتك على التعامل مع تعقيدات الحياة العصرية بوضوح وهدف.", DataType = "Text" },

                // Section Headers (#services, #projects, #testimonials, recent articles)
                new() { PageKey = "home", SectionKey = "services_badge", ValueEn = "Enterprise Solutions", ValueAr = "الخدمات والحلول الرقمية", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "services_title", ValueEn = "", ValueAr = "", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "services_title_highlight", ValueEn = "Platforms", ValueAr = "الاستراتيجية", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "services_subtitle", ValueEn = "", ValueAr = "", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "services_card_cta", ValueEn = "Explore Solution", ValueAr = "استكشف المزيد", DataType = "Text" },

                new() { PageKey = "home", SectionKey = "projects_badge", ValueEn = "Featured Enterprise Portfolio", ValueAr = "المشاريع والحلول المنفذة", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "projects_title", ValueEn = "Proven Track Record & Case Studies", ValueAr = "قصص نجاح ومشاريع رائدة", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "projects_subtitle", ValueEn = "Showcasing mission-critical enterprise systems and AI deployments delivered for high-growth organizations.", ValueAr = "نماذج من المنصات والأنظمة المتطورة التي تم تطويرها لتمكين الشركاء والعملاء المؤسسيين.", DataType = "Text" },

                new() { PageKey = "home", SectionKey = "testimonials_badge", ValueEn = "Client Endorsements & Reviews", ValueAr = "آراء العملاء والشركاء", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "testimonials_title", ValueEn = "Trusted by Visionary Enterprise Leaders", ValueAr = "ثقة رواد الأعمال والمؤسسات", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "testimonials_subtitle", ValueEn = "Hear how our partners achieve market clarity and accelerated execution through our intelligent platforms.", ValueAr = "شهادات من شركائنا وعملائنا حول القيمة الاستراتيجية والتحول الرقمي الذي أحدثته منظومتنا.", DataType = "Text" },

                new() { PageKey = "home", SectionKey = "articles_badge", ValueEn = "Daleel Intelligence Feed", ValueAr = "نشرة دليل الاستراتيجية", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "articles_title", ValueEn = "Global Perspectives & Strategic Insights", ValueAr = "رؤى عالمية وتحليلات استراتيجية", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "articles_subtitle", ValueEn = "High-conviction intelligence, platform announcements, and macroeconomic research from Daleel's analysts.", ValueAr = "تحليلات معمقة وأخبار حصرية وتقارير اقتصادية من نخبة خبراء ومحللي دليل.", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "articles_link_text", ValueEn = "View All Insights", ValueAr = "عرض جميع المقالات", DataType = "Text" },
                new() { PageKey = "home", SectionKey = "articles_card_cta", ValueEn = "Read Article", ValueAr = "اقرأ المزيد", DataType = "Text" },

                // About page — heading and intro also come from the Brand Profile.
                new() { PageKey = "about", SectionKey = "about_title", ValueEn = "", ValueAr = "", DataType = "Text" },
                new() { PageKey = "about", SectionKey = "about_desc", ValueEn = "", ValueAr = "", DataType = "Html" },
                new() { PageKey = "about", SectionKey = "about_hero_image", ValueEn = "/images/homepage/ai_predictive_1777586859142.png", ValueAr = "/images/homepage/ai_predictive_1777586859142.png", DataType = "ImagePath" },

                // Trust page — the only pair backed by genuinely static copy, so the defaults
                // carry that exact text and the page renders identically either way.
                new() { PageKey = "trust", SectionKey = "trust_title", ValueEn = "Trust & Governance", ValueAr = "الثقة والحوكمة", DataType = "Text" },
                new() { PageKey = "trust", SectionKey = "trust_desc", ValueEn = "Daleel is built on a foundation of ethical governance and strict regulatory standards. Every aspect of our platform - from information to commerce to media - operates under clear rules and conditions designed to preserve the integrity of human interaction and decision-making.", ValueAr = "دليل مبني على أساس من الحوكمة الأخلاقية والمعايير التنظيمية الصارمة. كل جانب من منصتنا - من المعلومات إلى التجارة إلى الإعلام - يعمل تحت قواعد وشروط واضحة مصممة للحفاظ على نزاهة التفاعل البشري وصناعة القرار.", DataType = "Text" },

                // Platforms page — heading interpolates the brand name, intro is brand copy.
                new() { PageKey = "platforms", SectionKey = "platforms_title", ValueEn = "", ValueAr = "", DataType = "Text" },
                new() { PageKey = "platforms", SectionKey = "platforms_desc", ValueEn = "", ValueAr = "", DataType = "Text" },

                // Global Marketplace (/Home/Shop). Defaults mirror the SharedResource strings the
                // page rendered before it became editable, so seeding changes nothing on screen.
                new() { PageKey = "shop", SectionKey = "shop_badge", ValueEn = "Global Marketplace", ValueAr = "سوق عالمي", DataType = "Text" },
                new() { PageKey = "shop", SectionKey = "shop_title", ValueEn = "Coming Soon", ValueAr = "قريباً", DataType = "Text" },
                new() { PageKey = "shop", SectionKey = "shop_desc", ValueEn = "We are building a global marketplace encompassing the full spectrum of human trade with specialized sub-platforms for Agriculture, Industry, Real Estate, Automotive, and Heavy Machinery & Equipment, designed to facilitate international commerce using future-oriented technologies.", ValueAr = "نحن نبني سوقا عالميا يشمل كامل طيف التجارة البشرية -- مع منصات فرعية متخصصة للزراعة والصناعة والعقارات والسيارات والمعدات الثقيلة -- مصمم لتسهيل التجارة الدولية باستخدام تقنيات مستقبلية التوجه.", DataType = "Text" },
                new() { PageKey = "shop", SectionKey = "shop_cta", ValueEn = "Return Home", ValueAr = "العودة للرئيسية", DataType = "Text" },

                // Blog & Insights — the blog landing page's fixed hero only. The articles
                // themselves stay in Articles & Blog (CmsArticleController).
                new() { PageKey = "blog", SectionKey = "blog_badge", ValueEn = "Daleel Newsroom & Insights", ValueAr = "دليل للمعرفة والأخبار", DataType = "Text" },
                new() { PageKey = "blog", SectionKey = "blog_title", ValueEn = "Latest Insights & Updates", ValueAr = "رؤى وأخبار المنظومة الرقمية", DataType = "Text" },
                new() { PageKey = "blog", SectionKey = "blog_desc", ValueEn = "Explore expert perspectives, platform announcements, and business intelligence strategies.", ValueAr = "استكشف أحدث المقالات والتحليلات والتقارير التقنية لدعم نمو مؤسستك في السوق السعودي.", DataType = "Text" },

                // Contact Us — headings and copy only. The enquiry form keeps its own
                // validation and lead-capture logic and is not driven by page sections.
                new() { PageKey = "contact", SectionKey = "contact_badge", ValueEn = "Get in Touch", ValueAr = "تواصل معنا", DataType = "Text" },
                new() { PageKey = "contact", SectionKey = "contact_title", ValueEn = "Contact ", ValueAr = "تواصل مع ", DataType = "Text" },
                new() { PageKey = "contact", SectionKey = "contact_desc", ValueEn = "Ready to elevate your strategic and analytical capabilities? Our global team is available to discuss your strategic needs.", ValueAr = "هل أنت مستعد لرفع مستوى قدراتك الاستراتيجية والتحليلية؟ فريقنا العالمي متاح على مدار الساعة لمناقشة احتياجاتك الاستراتيجية.", DataType = "Text" },
                new() { PageKey = "contact", SectionKey = "contact_direct_title", ValueEn = "Reach Out Directly", ValueAr = "تواصل مباشرة", DataType = "Text" },

                // Soon — one shared placeholder page. Every "coming soon" footer link
                // (Solutions, Intelligence Feed, Risk Dashboard, API Access, Integrations)
                // routes to /Home/Soon, so a single card governs all of them.
                new() { PageKey = "soon", SectionKey = "soon_badge", ValueEn = "Under Construction", ValueAr = "قيد الإنشاء", DataType = "Text" },
                new() { PageKey = "soon", SectionKey = "soon_title", ValueEn = "Coming Soon", ValueAr = "قريباً", DataType = "Text" },
                new() { PageKey = "soon", SectionKey = "soon_desc", ValueEn = "We are currently crafting something extraordinary. This feature is being built and will be available shortly.", ValueAr = "نحن حالياً نصنع شيئاً استثنائياً. هذه الميزة يتم بناؤها بدقة استراتيجية فائقة وستكون متاحة قريباً.", DataType = "Text" },
                new() { PageKey = "soon", SectionKey = "soon_cta", ValueEn = "Return Home", ValueAr = "العودة للرئيسية", DataType = "Text" }
            };
        }

        private void InvalidatePageCache(string pageKey)
        {
            _cache.Remove($"{CachePrefix}{pageKey}_en");
            _cache.Remove($"{CachePrefix}{pageKey}_ar");
        }

        private static PageSectionDto ToDto(PageSection p) => new()
        {
            Id = p.Id,
            PageKey = p.PageKey,
            SectionKey = p.SectionKey,
            ValueEn = p.ValueEn,
            ValueAr = p.ValueAr,
            DataType = p.DataType,
            UpdatedAt = p.UpdatedAt
        };
    }
}
