using System.Linq.Expressions;

using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daleel.BAL.Services
{
    public class CmsProjectService : ICmsProjectService
    {
        private readonly ApplicationDbContext _db;

        private static readonly Expression<Func<CmsProject, CmsProjectListItemDto>> ToListItem = p => new CmsProjectListItemDto
        {
            Id = p.Id,
            BrandId = p.BrandId,
            BrandNameEn = p.Brand != null ? p.Brand.NameEn : string.Empty,
            BrandNameAr = p.Brand != null ? p.Brand.NameAr : string.Empty,
            BrandSlug = p.Brand != null ? p.Brand.Slug : string.Empty,
            Slug = p.Slug,
            NameEn = p.NameEn,
            NameAr = p.NameAr,
            ShortDescEn = p.ShortDescEn,
            ShortDescAr = p.ShortDescAr,
            ClientNameEn = p.ClientNameEn,
            ClientNameAr = p.ClientNameAr,
            CompletionDate = p.CompletionDate,
            ProjectUrl = p.ProjectUrl,
            ImagePath = p.ImagePath,
            DisplayOrder = p.DisplayOrder,
            IsPublished = p.IsPublished,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };

        private static readonly Expression<Func<CmsProject, CmsProjectDetailDto>> ToDetail = p => new CmsProjectDetailDto
        {
            Id = p.Id,
            BrandId = p.BrandId,
            BrandNameEn = p.Brand != null ? p.Brand.NameEn : string.Empty,
            BrandNameAr = p.Brand != null ? p.Brand.NameAr : string.Empty,
            BrandSlug = p.Brand != null ? p.Brand.Slug : string.Empty,
            Slug = p.Slug,
            NameEn = p.NameEn,
            NameAr = p.NameAr,
            ShortDescEn = p.ShortDescEn,
            ShortDescAr = p.ShortDescAr,
            DescriptionEn = p.DescriptionEn,
            DescriptionAr = p.DescriptionAr,
            ClientNameEn = p.ClientNameEn,
            ClientNameAr = p.ClientNameAr,
            CompletionDate = p.CompletionDate,
            ProjectUrl = p.ProjectUrl,
            ImagePath = p.ImagePath,
            DisplayOrder = p.DisplayOrder,
            IsPublished = p.IsPublished,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };

        public CmsProjectService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<CmsProjectListItemDto>> SearchAsync(CmsProjectQuery query)
        {
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var dbQuery = _db.CmsProjects
                .AsNoTracking()
                .Include(p => p.Brand)
                .AsQueryable();

            if (query.BrandId.HasValue && query.BrandId.Value > 0)
            {
                dbQuery = dbQuery.Where(p => p.BrandId == query.BrandId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(query.BrandSlug))
            {
                var normalizedBrandSlug = query.BrandSlug.Trim().ToLowerInvariant();
                dbQuery = dbQuery.Where(p => p.Brand != null && p.Brand.Slug == normalizedBrandSlug);
            }

            if (query.IsPublished.HasValue)
            {
                dbQuery = dbQuery.Where(p => p.IsPublished == query.IsPublished.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var term = query.Q.Trim();
                dbQuery = dbQuery.Where(p =>
                    p.NameEn.Contains(term) ||
                    p.NameAr.Contains(term) ||
                    (p.ClientNameEn != null && p.ClientNameEn.Contains(term)) ||
                    (p.ClientNameAr != null && p.ClientNameAr.Contains(term)) ||
                    (p.ShortDescEn != null && p.ShortDescEn.Contains(term)) ||
                    (p.ShortDescAr != null && p.ShortDescAr.Contains(term)));
            }

            var totalCount = await dbQuery.CountAsync();

            var items = await dbQuery
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.NameEn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToListItem)
                .ToListAsync();

            return new PagedResult<CmsProjectListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<IReadOnlyList<CmsProjectListItemDto>> GetByBrandAsync(int brandId, bool onlyPublished = false)
        {
            var query = _db.CmsProjects
                .AsNoTracking()
                .Include(p => p.Brand)
                .Where(p => p.BrandId == brandId);

            if (onlyPublished)
            {
                query = query.Where(p => p.IsPublished);
            }

            return await query
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.NameEn)
                .Select(ToListItem)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<CmsProjectListItemDto>> GetByBrandSlugAsync(string brandSlug, bool onlyPublished = false)
        {
            if (string.IsNullOrWhiteSpace(brandSlug)) return Array.Empty<CmsProjectListItemDto>();

            var normalizedSlug = brandSlug.Trim().ToLowerInvariant();
            var query = _db.CmsProjects
                .AsNoTracking()
                .Include(p => p.Brand)
                .Where(p => p.Brand != null && p.Brand.Slug == normalizedSlug);

            if (onlyPublished)
            {
                query = query.Where(p => p.IsPublished);
            }

            return await query
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.NameEn)
                .Select(ToListItem)
                .ToListAsync();
        }

        public async Task<CmsProjectDetailDto?> GetByIdAsync(int id)
        {
            return await _db.CmsProjects
                .AsNoTracking()
                .Include(p => p.Brand)
                .Where(p => p.Id == id)
                .Select(ToDetail)
                .FirstOrDefaultAsync();
        }

        public async Task<CmsProjectDetailDto?> GetBySlugAsync(int brandId, string slug, bool onlyPublished = false)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;

            var normalizedSlug = slug.Trim().ToLowerInvariant();
            var query = _db.CmsProjects
                .AsNoTracking()
                .Include(p => p.Brand)
                .Where(p => p.BrandId == brandId && p.Slug == normalizedSlug);

            if (onlyPublished)
            {
                query = query.Where(p => p.IsPublished);
            }

            return await query.Select(ToDetail).FirstOrDefaultAsync();
        }

        public async Task<ServiceResult<CmsProjectDetailDto>> CreateAsync(CmsProjectInput input)
        {
            var errors = await ValidateAsync(input);
            if (errors.Count > 0)
            {
                return ServiceResult<CmsProjectDetailDto>.Invalid(errors.ToArray());
            }

            var slug = await GenerateUniqueSlugAsync(input.BrandId, input.Slug, input.NameEn, null);

            var entity = new CmsProject
            {
                BrandId = input.BrandId,
                Slug = slug,
                NameEn = input.NameEn.Trim(),
                NameAr = input.NameAr.Trim(),
                ShortDescEn = Text.Clean(input.ShortDescEn),
                ShortDescAr = Text.Clean(input.ShortDescAr),
                DescriptionEn = Text.Clean(input.DescriptionEn),
                DescriptionAr = Text.Clean(input.DescriptionAr),
                ClientNameEn = Text.Clean(input.ClientNameEn),
                ClientNameAr = Text.Clean(input.ClientNameAr),
                CompletionDate = input.CompletionDate,
                ProjectUrl = Text.Clean(input.ProjectUrl),
                ImagePath = Text.Clean(input.ImagePath),
                DisplayOrder = input.DisplayOrder,
                IsPublished = input.IsPublished,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.CmsProjects.Add(entity);
            await _db.SaveChangesAsync();

            var detail = await GetByIdAsync(entity.Id);
            return ServiceResult<CmsProjectDetailDto>.Success(detail!);
        }

        public async Task<ServiceResult<CmsProjectDetailDto>> UpdateAsync(int id, CmsProjectInput input)
        {
            var entity = await _db.CmsProjects.FirstOrDefaultAsync(p => p.Id == id);
            if (entity is null)
            {
                return ServiceResult<CmsProjectDetailDto>.Missing();
            }

            var errors = await ValidateAsync(input);
            if (errors.Count > 0)
            {
                return ServiceResult<CmsProjectDetailDto>.Invalid(errors.ToArray());
            }

            var slug = await GenerateUniqueSlugAsync(input.BrandId, input.Slug, input.NameEn, id);

            entity.BrandId = input.BrandId;
            entity.Slug = slug;
            entity.NameEn = input.NameEn.Trim();
            entity.NameAr = input.NameAr.Trim();
            entity.ShortDescEn = Text.Clean(input.ShortDescEn);
            entity.ShortDescAr = Text.Clean(input.ShortDescAr);
            entity.DescriptionEn = Text.Clean(input.DescriptionEn);
            entity.DescriptionAr = Text.Clean(input.DescriptionAr);
            entity.ClientNameEn = Text.Clean(input.ClientNameEn);
            entity.ClientNameAr = Text.Clean(input.ClientNameAr);
            entity.CompletionDate = input.CompletionDate;
            entity.ProjectUrl = Text.Clean(input.ProjectUrl);
            entity.ImagePath = Text.Clean(input.ImagePath);
            entity.DisplayOrder = input.DisplayOrder;
            entity.IsPublished = input.IsPublished;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var detail = await GetByIdAsync(entity.Id);
            return ServiceResult<CmsProjectDetailDto>.Success(detail!);
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var entity = await _db.CmsProjects.FirstOrDefaultAsync(p => p.Id == id);
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
            var entity = await _db.CmsProjects.FirstOrDefaultAsync(p => p.Id == id);
            if (entity is null)
            {
                return ServiceResult<bool>.Missing();
            }

            entity.IsPublished = !entity.IsPublished;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ServiceResult<bool>.Success(entity.IsPublished);
        }

        public async Task SeedDefaultProjectsIfEmptyAsync()
        {
            if (await _db.CmsProjects.AnyAsync())
            {
                return;
            }

            var daleelBrand = await _db.BrandProfiles.FirstOrDefaultAsync(b => b.Slug == "daleel");
            var neurixBrand = await _db.BrandProfiles.FirstOrDefaultAsync(b => b.Slug == "neurix");

            if (daleelBrand == null) return;

            var projects = new List<CmsProject>
            {
                // Daleel Projects
                new()
                {
                    BrandId = daleelBrand.Id,
                    Slug = "saudi-telemetry-hub",
                    NameEn = "National Enterprise Telemetry Hub",
                    NameAr = "المنصة الوطنية للقياس والتحليلات المؤسسية",
                    ShortDescEn = "Consolidated telemetry and real-time operational dashboard for enterprise logistics in Riyadh.",
                    ShortDescAr = "منصة موحدة لقياس ومتابعة العمليات اللوجستية الفورية للشركات الكبرى في الرياض.",
                    DescriptionEn = "Designed and deployed an integrated business intelligence system connecting 14 enterprise branches with sub-second data streaming and automated executive reporting.",
                    DescriptionAr = "تصميم وتدشين نظام ذكاء أعمال متكامل يربط 14 فرعاً مؤسسياً مع تدفق فوري للبيانات وإعداد تقارير تنفيذية مؤتمتة.",
                    ClientNameEn = "Riyadh Logistics Group",
                    ClientNameAr = "مجموعة الرياض للخدمات اللوجستية",
                    CompletionDate = DateTime.UtcNow.AddMonths(-3),
                    ProjectUrl = "https://aidaleel.com",
                    DisplayOrder = 1,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    BrandId = daleelBrand.Id,
                    Slug = "fintech-pipeline-engine",
                    NameEn = "FinTech Deal & Pipeline Management",
                    NameAr = "منظومة إدارة الصفقات والتدفق المالي للتقنية المالية",
                    ShortDescEn = "Tailored CRM and deal tracking engine for high-velocity investment pipelines.",
                    ShortDescAr = "محرك مخصص لإدارة علاقات العملاء ومسارات الاستثمار والصفقات عالية الوتيرة.",
                    DescriptionEn = "Automated customer acquisition pipelines, KYC document validation, and deal stage scoring for a prominent Saudi investment firm.",
                    DescriptionAr = "أتمتة مسارات استقطاب العملاء، والتحقق من وثائق 'اعرف عميلك'، وتقييم مراحل الصفقات لصالح شركة استثمارية رائدة في المملكة.",
                    ClientNameEn = "Afaq Capital",
                    ClientNameAr = "آفاق كابيتال",
                    CompletionDate = DateTime.UtcNow.AddMonths(-1),
                    ProjectUrl = "https://aidaleel.com",
                    DisplayOrder = 2,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            if (neurixBrand != null)
            {
                projects.AddRange(new[]
                {
                    new CmsProject
                    {
                        BrandId = neurixBrand.Id,
                        Slug = "neural-market-swarm",
                        NameEn = "Neural Market Prediction Engine",
                        NameAr = "محرك التنبؤ العصبي بحركة الأسواق",
                        ShortDescEn = "Deep learning time-series neural models forecasting commodity price fluctuations.",
                        ShortDescAr = "نماذج شبكات عصبية عميقة للتنبؤ بالسلاسل الزمنية وتقلبات أسعار السلع والأسواق.",
                        DescriptionEn = "Custom transformer architectures processing multi-source global news and financial tickers to forecast asset volatility with 94.2% directional accuracy.",
                        DescriptionAr = "هندسة نماذج المحولات العصبية لتحليل الأخارف والبيانات المالية العالمية والتنبؤ بتقلبات الأصول بدقة اتجاهية بلغت 94.2%.",
                        ClientNameEn = "Gulf Trading Systems",
                        ClientNameAr = "أنظمة الخليج للتداول",
                        CompletionDate = DateTime.UtcNow.AddMonths(-2),
                        ProjectUrl = "https://neurix.ai",
                        DisplayOrder = 1,
                        IsPublished = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                });
            }

            await _db.CmsProjects.AddRangeAsync(projects);
            await _db.SaveChangesAsync();
        }

        private async Task<List<ServiceError>> ValidateAsync(CmsProjectInput input)
        {
            var errors = new List<ServiceError>();

            if (input.BrandId <= 0 || !await _db.BrandProfiles.AnyAsync(b => b.Id == input.BrandId))
            {
                errors.Add(new ServiceError(nameof(input.BrandId), "Select a valid brand profile."));
            }

            if (string.IsNullOrWhiteSpace(input.NameEn))
            {
                errors.Add(new ServiceError(nameof(input.NameEn), "English project name is required."));
            }

            if (string.IsNullOrWhiteSpace(input.NameAr))
            {
                errors.Add(new ServiceError(nameof(input.NameAr), "Arabic project name is required."));
            }

            return errors;
        }

        private async Task<string> GenerateUniqueSlugAsync(int brandId, string? requestedSlug, string nameEn, int? currentId)
        {
            string baseSlug = string.IsNullOrWhiteSpace(requestedSlug)
                ? SlugHelper.Slugify(nameEn)
                : SlugHelper.Slugify(requestedSlug);

            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                baseSlug = "project-" + Guid.NewGuid().ToString("N")[..8];
            }

            var slug = baseSlug;
            var counter = 1;

            while (true)
            {
                var query = _db.CmsProjects.AsNoTracking().Where(p => p.BrandId == brandId && p.Slug == slug);
                if (currentId.HasValue)
                {
                    query = query.Where(p => p.Id != currentId.Value);
                }

                var exists = await query.AnyAsync();
                if (!exists)
                {
                    return slug;
                }

                slug = $"{baseSlug}-{counter++}";
            }
        }

    }
}
