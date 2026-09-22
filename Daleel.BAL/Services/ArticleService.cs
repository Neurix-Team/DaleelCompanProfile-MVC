using System.Linq.Expressions;
using System.Net;
using System.Text.RegularExpressions;
using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;

namespace Daleel.BAL.Services
{
    public class ArticleService : IArticleService
    {
        private readonly ApplicationDbContext _db;
        private static readonly HtmlSanitizer Sanitizer = new();

        private static readonly Expression<Func<Article, ArticleListItem>> ToListItem = a => new ArticleListItem
        {
            Id = a.Id,
            Slug = a.Slug,
            TitleEn = a.TitleEn,
            TitleAr = a.TitleAr,
            SummaryEn = a.SummaryEn,
            SummaryAr = a.SummaryAr,
            CoverImagePath = a.CoverImagePath,
            Category = a.Category,
            Tags = a.Tags,
            AuthorName = a.AuthorName,
            ReadingMinutes = a.ReadingMinutes,
            IsPublished = a.IsPublished,
            PublishedAt = a.PublishedAt,
            ScheduledPublishAt = a.ScheduledPublishAt,
            CreatedAt = a.CreatedAt
        };

        public ArticleService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<ArticleListItem>> SearchAsync(ArticleQuery query)
        {
            var q = _db.Articles.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var term = query.Q.Trim();
                q = q.Where(a => a.TitleEn.Contains(term) ||
                                 a.TitleAr.Contains(term) ||
                                 (a.SummaryEn != null && a.SummaryEn.Contains(term)) ||
                                 (a.SummaryAr != null && a.SummaryAr.Contains(term)) ||
                                 (a.Category != null && a.Category.Contains(term)) ||
                                 (a.Tags != null && a.Tags.Contains(term)) ||
                                 a.AuthorName.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.Category) && !string.Equals(query.Category.Trim(), "All", StringComparison.OrdinalIgnoreCase))
            {
                var cat = query.Category.Trim();
                q = q.Where(a => a.Category == cat);
            }

            if (!string.IsNullOrWhiteSpace(query.Tag))
            {
                var tag = query.Tag.Trim();
                q = q.Where(a => a.Tags != null && a.Tags.Contains(tag));
            }

            if (query.IsPublished.HasValue)
            {
                if (query.IsPublished.Value)
                {
                    var now = DateTime.UtcNow;
                    q = q.Where(a => a.IsPublished && (a.ScheduledPublishAt == null || a.ScheduledPublishAt <= now));
                }
                else
                {
                    q = q.Where(a => !a.IsPublished);
                }
            }

            var total = await q.CountAsync();
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var items = await q
                .OrderByDescending(a => a.PublishedAt ?? a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToListItem)
                .ToListAsync();

            return new PagedResult<ArticleListItem>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task<IReadOnlyList<ArticleListItem>> GetRecentPublishedAsync(int count = 6)
        {
            var now = DateTime.UtcNow;
            return await _db.Articles
                .AsNoTracking()
                .Where(a => a.IsPublished && (a.ScheduledPublishAt == null || a.ScheduledPublishAt <= now))
                .OrderByDescending(a => a.PublishedAt ?? a.CreatedAt)
                .Take(count)
                .Select(ToListItem)
                .ToListAsync();
        }

        public async Task<ArticleDetailDto?> GetBySlugAsync(string slug, bool onlyPublished = true)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;

            var q = _db.Articles.AsNoTracking().Where(a => a.Slug == slug.Trim());
            if (onlyPublished)
            {
                var now = DateTime.UtcNow;
                q = q.Where(a => a.IsPublished && (a.ScheduledPublishAt == null || a.ScheduledPublishAt <= now));
            }

            var entity = await q.FirstOrDefaultAsync();
            return entity is null ? null : ToDetailDto(entity);
        }

        public async Task<ArticleDetailDto?> GetByIdAsync(int id)
        {
            var entity = await _db.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            return entity is null ? null : ToDetailDto(entity);
        }

        public async Task<ServiceResult<ArticleDetailDto>> CreateAsync(ArticleInput input)
        {
            var bodyHtmlEn = Sanitizer.Sanitize(input.BodyHtmlEn?.Trim() ?? string.Empty);
            var bodyHtmlAr = Sanitizer.Sanitize(input.BodyHtmlAr?.Trim() ?? string.Empty);
            var errors = ValidateInput(input, bodyHtmlEn, bodyHtmlAr);
            if (errors.Count > 0)
            {
                return ServiceResult<ArticleDetailDto>.Invalid(errors.ToArray());
            }

            var slug = await GenerateUniqueSlugAsync(input.Slug, input.TitleEn, null);

            var entity = new Article
            {
                Slug = slug,
                TitleEn = input.TitleEn.Trim(),
                TitleAr = input.TitleAr.Trim(),
                SummaryEn = Text.Clean(input.SummaryEn),
                SummaryAr = Text.Clean(input.SummaryAr),
                BodyHtmlEn = bodyHtmlEn,
                BodyHtmlAr = bodyHtmlAr,
                CoverImagePath = Text.Clean(input.CoverImagePath),
                Category = Text.Clean(input.Category),
                Tags = Text.Clean(input.Tags),
                AuthorName = string.IsNullOrWhiteSpace(input.AuthorName) ? "Daleel Team" : input.AuthorName.Trim(),
                ReadingMinutes = Math.Max(1, input.ReadingMinutes),
                IsPublished = input.IsPublished,
                PublishedAt = input.IsPublished ? DateTime.UtcNow : null,
                ScheduledPublishAt = input.ScheduledPublishAt,
                MetaTitleEn = Text.Clean(input.MetaTitleEn),
                MetaTitleAr = Text.Clean(input.MetaTitleAr),
                MetaDescriptionEn = Text.Clean(input.MetaDescriptionEn),
                MetaDescriptionAr = Text.Clean(input.MetaDescriptionAr),
                CanonicalUrl = Text.Clean(input.CanonicalUrl),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Articles.Add(entity);
            await _db.SaveChangesAsync();

            return ServiceResult<ArticleDetailDto>.Success(ToDetailDto(entity));
        }

        public async Task<ServiceResult<ArticleDetailDto>> UpdateAsync(int id, ArticleInput input)
        {
            var entity = await _db.Articles.FirstOrDefaultAsync(a => a.Id == id);
            if (entity is null)
            {
                return ServiceResult<ArticleDetailDto>.Missing();
            }

            var bodyHtmlEn = Sanitizer.Sanitize(input.BodyHtmlEn?.Trim() ?? string.Empty);
            var bodyHtmlAr = Sanitizer.Sanitize(input.BodyHtmlAr?.Trim() ?? string.Empty);
            var errors = ValidateInput(input, bodyHtmlEn, bodyHtmlAr);
            if (errors.Count > 0)
            {
                return ServiceResult<ArticleDetailDto>.Invalid(errors.ToArray());
            }

            var slug = await GenerateUniqueSlugAsync(input.Slug, input.TitleEn, id);

            entity.Slug = slug;
            entity.TitleEn = input.TitleEn.Trim();
            entity.TitleAr = input.TitleAr.Trim();
            entity.SummaryEn = Text.Clean(input.SummaryEn);
            entity.SummaryAr = Text.Clean(input.SummaryAr);
            entity.BodyHtmlEn = bodyHtmlEn;
            entity.BodyHtmlAr = bodyHtmlAr;
            entity.CoverImagePath = Text.Clean(input.CoverImagePath);
            entity.Category = Text.Clean(input.Category);
            entity.Tags = Text.Clean(input.Tags);
            entity.AuthorName = string.IsNullOrWhiteSpace(input.AuthorName) ? "Daleel Team" : input.AuthorName.Trim();
            entity.ReadingMinutes = Math.Max(1, input.ReadingMinutes);
            
            if (input.IsPublished && !entity.IsPublished)
            {
                entity.PublishedAt = DateTime.UtcNow;
            }
            entity.IsPublished = input.IsPublished;
            entity.ScheduledPublishAt = input.ScheduledPublishAt;

            entity.MetaTitleEn = Text.Clean(input.MetaTitleEn);
            entity.MetaTitleAr = Text.Clean(input.MetaTitleAr);
            entity.MetaDescriptionEn = Text.Clean(input.MetaDescriptionEn);
            entity.MetaDescriptionAr = Text.Clean(input.MetaDescriptionAr);
            entity.CanonicalUrl = Text.Clean(input.CanonicalUrl);

            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return ServiceResult<ArticleDetailDto>.Success(ToDetailDto(entity));
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var entity = await _db.Articles.FirstOrDefaultAsync(a => a.Id == id);
            if (entity is null)
            {
                return ServiceResult.Missing();
            }

            _db.Articles.Remove(entity);
            await _db.SaveChangesAsync();

            return ServiceResult.Success();
        }

        public async Task<ServiceResult<bool>> TogglePublishAsync(int id)
        {
            var entity = await _db.Articles.FirstOrDefaultAsync(a => a.Id == id);
            if (entity is null)
            {
                return ServiceResult<bool>.Missing();
            }

            entity.IsPublished = !entity.IsPublished;
            if (entity.IsPublished && entity.PublishedAt is null)
            {
                entity.PublishedAt = DateTime.UtcNow;
            }
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return ServiceResult<bool>.Success(entity.IsPublished);
        }

        public async Task<CmsDashboardStats> GetDashboardStatsAsync()
        {
            var totalArticles = await _db.Articles.CountAsync();
            var publishedArticles = await _db.Articles.CountAsync(a => a.IsPublished);
            var totalPageSections = await _db.PageSections.CountAsync();
            var totalBrands = await _db.BrandProfiles.CountAsync();
            var publishedBrands = await _db.BrandProfiles.CountAsync(b => b.IsPublished);
            var totalServices = await _db.CmsServices.CountAsync();
            var totalProjects = await _db.CmsProjects.CountAsync();
            var totalTeam = await _db.TeamMembers.CountAsync();
            var totalTestimonials = await _db.Testimonials.CountAsync();

            var recentArticles = await _db.Articles
                .AsNoTracking()
                .OrderByDescending(a => a.UpdatedAt)
                .Take(5)
                .Select(ToListItem)
                .ToListAsync();

            var brands = await _db.BrandProfiles
                .AsNoTracking()
                .OrderBy(b => b.NameEn)
                .Select(b => new BrandProfileListItemDto
                {
                    Id = b.Id,
                    Slug = b.Slug,
                    NameEn = b.NameEn,
                    NameAr = b.NameAr,
                    TaglineEn = b.TaglineEn,
                    TaglineAr = b.TaglineAr,
                    LogoPath = b.LogoPath,
                    LogoDarkPath = b.LogoDarkPath,
                    FaviconPath = b.FaviconPath,
                    PrimaryColor = b.PrimaryColor,
                    SecondaryColor = b.SecondaryColor,
                    AccentColor = b.AccentColor,
                    Email = b.Email,
                    Phone = b.Phone,
                    Website = b.Website,
                    IsPublished = b.IsPublished,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                })
                .ToListAsync();

            return new CmsDashboardStats
            {
                TotalArticles = totalArticles,
                PublishedArticles = publishedArticles,
                DraftArticles = totalArticles - publishedArticles,
                TotalPageSections = totalPageSections,
                TotalBrands = totalBrands,
                PublishedBrands = publishedBrands,
                TotalServices = totalServices,
                TotalProjects = totalProjects,
                TotalTeamMembers = totalTeam,
                TotalTestimonials = totalTestimonials,
                RecentArticles = recentArticles,
                Brands = brands
            };
        }

        public async Task<IReadOnlyList<string>> GetDistinctCategoriesAsync()
        {
            var now = DateTime.UtcNow;
            return await _db.Articles
                .AsNoTracking()
                .Where(a => a.IsPublished && (a.ScheduledPublishAt == null || a.ScheduledPublishAt <= now) && a.Category != null && a.Category != "")
                .Select(a => a.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<string>> GetDistinctTagsAsync()
        {
            var now = DateTime.UtcNow;
            var tagStrings = await _db.Articles
                .AsNoTracking()
                .Where(a => a.IsPublished && (a.ScheduledPublishAt == null || a.ScheduledPublishAt <= now) && !string.IsNullOrWhiteSpace(a.Tags))
                .Select(a => a.Tags!)
                .ToListAsync();

            var tags = tagStrings
                .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t)
                .ToList();

            return tags;
        }

        private static ArticleDetailDto ToDetailDto(Article a) => new()
        {
            Id = a.Id,
            Slug = a.Slug,
            TitleEn = a.TitleEn,
            TitleAr = a.TitleAr,
            SummaryEn = a.SummaryEn,
            SummaryAr = a.SummaryAr,
            BodyHtmlEn = a.BodyHtmlEn,
            BodyHtmlAr = a.BodyHtmlAr,
            CoverImagePath = a.CoverImagePath,
            Category = a.Category,
            Tags = a.Tags,
            AuthorName = a.AuthorName,
            ReadingMinutes = a.ReadingMinutes,
            IsPublished = a.IsPublished,
            PublishedAt = a.PublishedAt,
            ScheduledPublishAt = a.ScheduledPublishAt,
            MetaTitleEn = a.MetaTitleEn,
            MetaTitleAr = a.MetaTitleAr,
            MetaDescriptionEn = a.MetaDescriptionEn,
            MetaDescriptionAr = a.MetaDescriptionAr,
            CanonicalUrl = a.CanonicalUrl,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt
        };

        private static List<ServiceError> ValidateInput(
            ArticleInput input,
            string sanitizedBodyHtmlEn,
            string sanitizedBodyHtmlAr)
        {
            var errors = new List<ServiceError>();

            if (string.IsNullOrWhiteSpace(input.TitleEn))
                errors.Add(new ServiceError(nameof(input.TitleEn), "English title is required."));

            if (string.IsNullOrWhiteSpace(input.TitleAr))
                errors.Add(new ServiceError(nameof(input.TitleAr), "Arabic title is required."));

            if (!HasMeaningfulHtmlContent(sanitizedBodyHtmlEn))
                errors.Add(new ServiceError(nameof(input.BodyHtmlEn), "English article content is required."));

            if (!HasMeaningfulHtmlContent(sanitizedBodyHtmlAr))
                errors.Add(new ServiceError(nameof(input.BodyHtmlAr), "Arabic article content is required."));

            return errors;
        }

        private static bool HasMeaningfulHtmlContent(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return false;
            }

            // Rich-text editors represent an empty document with markup such as
            // <p><br></p>. Strip markup and invisible characters before checking.
            var text = Regex.Replace(html, @"<[^>]*>", string.Empty);
            text = WebUtility.HtmlDecode(text)
                .Replace('\u00A0', ' ')
                .Replace("\u200B", string.Empty)
                .Replace("\uFEFF", string.Empty);

            if (!string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            // An embedded image is also valid article content even without text.
            return Regex.IsMatch(html, @"<img\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private async Task<string> GenerateUniqueSlugAsync(string? requestedSlug, string titleEn, int? currentId)
        {
            string baseSlug = string.IsNullOrWhiteSpace(requestedSlug)
                ? Slugify(titleEn)
                : Slugify(requestedSlug);

            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                baseSlug = "article-" + Guid.NewGuid().ToString("N")[..8];
            }

            var slug = baseSlug;
            var counter = 1;

            while (true)
            {
                var query = _db.Articles.AsNoTracking().Where(a => a.Slug == slug);
                if (currentId.HasValue)
                {
                    query = query.Where(a => a.Id != currentId.Value);
                }

                var exists = await query.AnyAsync();
                if (!exists)
                {
                    return slug;
                }

                slug = $"{baseSlug}-{counter++}";
            }
        }

        private static string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var value = text.ToLowerInvariant().Trim();
            // Remove invalid characters
            value = Regex.Replace(value, @"[^a-z0-9\s-]", "");
            // Replace multiple spaces or hyphens with a single hyphen
            value = Regex.Replace(value, @"[\s-]+", "-").Trim('-');

            return value.Length > 200 ? value[..200].Trim('-') : value;
        }
    }
}
