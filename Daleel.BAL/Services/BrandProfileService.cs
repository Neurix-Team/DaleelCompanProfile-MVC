using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daleel.BAL.Services
{
    public class BrandProfileService : IBrandProfileService
    {
        private readonly ApplicationDbContext _db;

        private static readonly Expression<Func<BrandProfile, BrandProfileListItemDto>> ToListItem = b => new BrandProfileListItemDto
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
        };

        private static readonly Expression<Func<BrandProfile, BrandProfileDetailDto>> ToDetail = b => new BrandProfileDetailDto
        {
            Id = b.Id,
            Slug = b.Slug,
            NameEn = b.NameEn,
            NameAr = b.NameAr,
            TaglineEn = b.TaglineEn,
            TaglineAr = b.TaglineAr,
            DescriptionEn = b.DescriptionEn,
            DescriptionAr = b.DescriptionAr,
            LogoPath = b.LogoPath,
            LogoDarkPath = b.LogoDarkPath,
            FaviconPath = b.FaviconPath,
            PrimaryColor = b.PrimaryColor,
            SecondaryColor = b.SecondaryColor,
            AccentColor = b.AccentColor,
            MetaTitleEn = b.MetaTitleEn,
            MetaTitleAr = b.MetaTitleAr,
            MetaDescriptionEn = b.MetaDescriptionEn,
            MetaDescriptionAr = b.MetaDescriptionAr,
            GoogleAnalyticsId = b.GoogleAnalyticsId,
            LinkedInUrl = b.LinkedInUrl,
            TwitterUrl = b.TwitterUrl,
            FacebookUrl = b.FacebookUrl,
            InstagramUrl = b.InstagramUrl,
            Email = b.Email,
            Phone = b.Phone,
            Address = b.Address,
            Website = b.Website,
            IsPublished = b.IsPublished,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        };

        public BrandProfileService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<BrandProfileListItemDto>> GetAllAsync(bool onlyPublished = false)
        {
            var query = _db.BrandProfiles.AsNoTracking();

            if (onlyPublished)
            {
                query = query.Where(b => b.IsPublished);
            }

            return await query
                .OrderBy(b => b.NameEn)
                .Select(ToListItem)
                .ToListAsync();
        }

        public async Task<BrandProfileDetailDto?> GetByIdAsync(int id)
        {
            return await _db.BrandProfiles
                .AsNoTracking()
                .Where(b => b.Id == id)
                .Select(ToDetail)
                .FirstOrDefaultAsync();
        }

        public async Task<BrandProfileDetailDto?> GetBySlugAsync(string slug, bool onlyPublished = false)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;

            var normalizedSlug = slug.Trim().ToLowerInvariant();
            var query = _db.BrandProfiles.AsNoTracking().Where(b => b.Slug == normalizedSlug);

            if (onlyPublished)
            {
                query = query.Where(b => b.IsPublished);
            }

            return await query.Select(ToDetail).FirstOrDefaultAsync();
        }

        public async Task<ServiceResult<BrandProfileDetailDto>> CreateAsync(BrandProfileInput input)
        {
            var errors = Validate(input);
            if (errors.Count > 0)
            {
                return ServiceResult<BrandProfileDetailDto>.Invalid(errors.ToArray());
            }

            var slug = await GenerateUniqueSlugAsync(input.Slug, input.NameEn, null);

            var entity = new BrandProfile
            {
                Slug = slug,
                NameEn = input.NameEn.Trim(),
                NameAr = input.NameAr.Trim(),
                TaglineEn = Text.Clean(input.TaglineEn),
                TaglineAr = Text.Clean(input.TaglineAr),
                DescriptionEn = Text.Clean(input.DescriptionEn),
                DescriptionAr = Text.Clean(input.DescriptionAr),
                LogoPath = Text.Clean(input.LogoPath),
                LogoDarkPath = Text.Clean(input.LogoDarkPath),
                FaviconPath = Text.Clean(input.FaviconPath),
                PrimaryColor = Text.Clean(input.PrimaryColor),
                SecondaryColor = Text.Clean(input.SecondaryColor),
                AccentColor = Text.Clean(input.AccentColor),
                MetaTitleEn = Text.Clean(input.MetaTitleEn),
                MetaTitleAr = Text.Clean(input.MetaTitleAr),
                MetaDescriptionEn = Text.Clean(input.MetaDescriptionEn),
                MetaDescriptionAr = Text.Clean(input.MetaDescriptionAr),
                GoogleAnalyticsId = Text.Clean(input.GoogleAnalyticsId),
                LinkedInUrl = Text.Clean(input.LinkedInUrl),
                TwitterUrl = Text.Clean(input.TwitterUrl),
                FacebookUrl = Text.Clean(input.FacebookUrl),
                InstagramUrl = Text.Clean(input.InstagramUrl),
                Email = Text.Clean(input.Email),
                Phone = Text.Clean(input.Phone),
                Address = Text.Clean(input.Address),
                Website = Text.Clean(input.Website),
                IsPublished = input.IsPublished,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.BrandProfiles.Add(entity);
            await _db.SaveChangesAsync();

            var detail = await GetByIdAsync(entity.Id);
            return ServiceResult<BrandProfileDetailDto>.Success(detail!);
        }

        public async Task<ServiceResult<BrandProfileDetailDto>> UpdateAsync(int id, BrandProfileInput input)
        {
            var entity = await _db.BrandProfiles.FirstOrDefaultAsync(b => b.Id == id);
            if (entity is null)
            {
                return ServiceResult<BrandProfileDetailDto>.Missing();
            }

            var errors = Validate(input);
            if (errors.Count > 0)
            {
                return ServiceResult<BrandProfileDetailDto>.Invalid(errors.ToArray());
            }

            var slug = await GenerateUniqueSlugAsync(input.Slug, input.NameEn, id);

            entity.Slug = slug;
            entity.NameEn = input.NameEn.Trim();
            entity.NameAr = input.NameAr.Trim();
            entity.TaglineEn = Text.Clean(input.TaglineEn);
            entity.TaglineAr = Text.Clean(input.TaglineAr);
            entity.DescriptionEn = Text.Clean(input.DescriptionEn);
            entity.DescriptionAr = Text.Clean(input.DescriptionAr);
            entity.LogoPath = Text.Clean(input.LogoPath);
            entity.LogoDarkPath = Text.Clean(input.LogoDarkPath);
            entity.FaviconPath = Text.Clean(input.FaviconPath);
            entity.PrimaryColor = Text.Clean(input.PrimaryColor);
            entity.SecondaryColor = Text.Clean(input.SecondaryColor);
            entity.AccentColor = Text.Clean(input.AccentColor);
            entity.MetaTitleEn = Text.Clean(input.MetaTitleEn);
            entity.MetaTitleAr = Text.Clean(input.MetaTitleAr);
            entity.MetaDescriptionEn = Text.Clean(input.MetaDescriptionEn);
            entity.MetaDescriptionAr = Text.Clean(input.MetaDescriptionAr);
            entity.GoogleAnalyticsId = Text.Clean(input.GoogleAnalyticsId);
            entity.LinkedInUrl = Text.Clean(input.LinkedInUrl);
            entity.TwitterUrl = Text.Clean(input.TwitterUrl);
            entity.FacebookUrl = Text.Clean(input.FacebookUrl);
            entity.InstagramUrl = Text.Clean(input.InstagramUrl);
            entity.Email = Text.Clean(input.Email);
            entity.Phone = Text.Clean(input.Phone);
            entity.Address = Text.Clean(input.Address);
            entity.Website = Text.Clean(input.Website);
            entity.IsPublished = input.IsPublished;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var detail = await GetByIdAsync(entity.Id);
            return ServiceResult<BrandProfileDetailDto>.Success(detail!);
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var entity = await _db.BrandProfiles.FirstOrDefaultAsync(b => b.Id == id);
            if (entity is null)
            {
                return ServiceResult.Missing();
            }

            // Soft delete
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<bool>> TogglePublishAsync(int id)
        {
            var entity = await _db.BrandProfiles.FirstOrDefaultAsync(b => b.Id == id);
            if (entity is null)
            {
                return ServiceResult<bool>.Missing();
            }

            entity.IsPublished = !entity.IsPublished;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ServiceResult<bool>.Success(entity.IsPublished);
        }

        public async Task SeedDefaultBrandsIfEmptyAsync()
        {
            if (await _db.BrandProfiles.AnyAsync())
            {
                return;
            }

            var defaultBrands = new List<BrandProfile>
            {
                new()
                {
                    Slug = "daleel",
                    NameEn = "Daleel",
                    NameAr = "دليل",
                    TaglineEn = "Next-Generation Enterprise Intelligence & AI Platform",
                    TaglineAr = "المنظومة الرقمية الرائدة لذكاء الأعمال وإدارة المؤسسات وحلول الذكاء الاصطناعي",
                    DescriptionEn = "Daleel is a next-generation Enterprise Intelligence & AI Platform tailored for high-growth Saudi businesses and enterprise ecosystems.",
                    DescriptionAr = "دليل هي منصة متقدمة لذكاء الأعمال والحلول المؤسسية المبتكرة المصممة لدعم نمو الشركات والتحول الرقمي المتوافق مع رؤية السعودية 2030.",
                    LogoPath = "/assets/Logo/Daleel.png",
                    LogoDarkPath = "/assets/Logo/Daleel.png",
                    FaviconPath = "/favicon.svg",
                    PrimaryColor = "#00B2EC",
                    SecondaryColor = "#10B981",
                    AccentColor = "#F9A01B",
                    MetaTitleEn = "Daleel - Enterprise AI & Digital Solutions",
                    MetaTitleAr = "دليل - المنظومة الرقمية الرائدة وحلول الذكاء الاصطناعي",
                    MetaDescriptionEn = "Daleel provides comprehensive enterprise business intelligence and AI solutions.",
                    MetaDescriptionAr = "تقدم دليل حلول ذكاء الأعمال والذكاء الاصطناعي المتطورة للمؤسسات.",
                    LinkedInUrl = "https://linkedin.com/company/daleel",
                    TwitterUrl = "https://x.com/daleel_ai",
                    Email = "info@aidaleel.com",
                    Phone = "+966 11 000 0000",
                    Address = "Riyadh, Kingdom of Saudi Arabia",
                    Website = "https://aidaleel.com",
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    Slug = "neurix",
                    NameEn = "Neurix",
                    NameAr = "نيوريكس",
                    TaglineEn = "Advanced Neural Systems & AI Solutions",
                    TaglineAr = "أنظمة عصبية وحلول ذكاء اصطناعي متقدمة",
                    DescriptionEn = "Neurix provides cutting-edge neural computation models, predictive analytics, and deep learning architectures for enterprise automation.",
                    DescriptionAr = "نيوريكس تقدم نماذج حوسبة عصبية وتحليلات تنبؤية وهندسة تعلم عميق متطورة لأتمتة الأعمال والمنظومات الذكية.",
                    LogoPath = "/assets/Logo/Daleel.png",
                    LogoDarkPath = "/assets/Logo/Daleel.png",
                    FaviconPath = "/favicon.svg",
                    PrimaryColor = "#6366F1",
                    SecondaryColor = "#8B5CF6",
                    AccentColor = "#EC4899",
                    MetaTitleEn = "Neurix - Advanced Neural Intelligence",
                    MetaTitleAr = "نيوريكس - أنظمة وحلول الذكاء الاصطناعي المتطورة",
                    MetaDescriptionEn = "Cutting-edge neural computation and predictive analytics for enterprises.",
                    MetaDescriptionAr = "أنظمة الحوسبة العصبية والتحليلات التنبؤية المتطورة للمؤسسات.",
                    LinkedInUrl = "https://linkedin.com/company/neurix-ai",
                    TwitterUrl = "https://x.com/neurix_ai",
                    Email = "contact@neurix.ai",
                    Phone = "+966 11 111 1111",
                    Address = "Riyadh, Kingdom of Saudi Arabia",
                    Website = "https://neurix.ai",
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await _db.BrandProfiles.AddRangeAsync(defaultBrands);
            await _db.SaveChangesAsync();
        }

        private static List<ServiceError> Validate(BrandProfileInput input)
        {
            var errors = new List<ServiceError>();

            if (string.IsNullOrWhiteSpace(input.NameEn))
                errors.Add(new ServiceError(nameof(input.NameEn), "English brand name is required."));

            if (string.IsNullOrWhiteSpace(input.NameAr))
                errors.Add(new ServiceError(nameof(input.NameAr), "Arabic brand name is required."));

            if (!string.IsNullOrWhiteSpace(input.Email) && !Regex.IsMatch(input.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors.Add(new ServiceError(nameof(input.Email), "Enter a valid email address."));

            return errors;
        }

        private async Task<string> GenerateUniqueSlugAsync(string? requestedSlug, string nameEn, int? currentId)
        {
            string baseSlug = string.IsNullOrWhiteSpace(requestedSlug)
                ? Slugify(nameEn)
                : Slugify(requestedSlug);

            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                baseSlug = "brand-" + Guid.NewGuid().ToString("N")[..8];
            }

            var slug = baseSlug;
            var counter = 1;

            while (true)
            {
                var query = _db.BrandProfiles.AsNoTracking().Where(b => b.Slug == slug);
                if (currentId.HasValue)
                {
                    query = query.Where(b => b.Id != currentId.Value);
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
            value = Regex.Replace(value, @"[^a-z0-9\s-]", "");
            value = Regex.Replace(value, @"[\s-]+", "-").Trim('-');

            return value.Length > 100 ? value[..100].Trim('-') : value;
        }
    }
}
