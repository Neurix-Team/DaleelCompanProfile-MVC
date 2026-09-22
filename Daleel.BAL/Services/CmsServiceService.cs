using System.Linq.Expressions;

using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daleel.BAL.Services
{
    public class CmsServiceService : ICmsServiceService
    {
        private readonly ApplicationDbContext _db;

        private static readonly Expression<Func<CmsService, CmsServiceListItemDto>> ToListItem = s => new CmsServiceListItemDto
        {
            Id = s.Id,
            BrandId = s.BrandId,
            BrandNameEn = s.Brand != null ? s.Brand.NameEn : string.Empty,
            BrandNameAr = s.Brand != null ? s.Brand.NameAr : string.Empty,
            BrandSlug = s.Brand != null ? s.Brand.Slug : string.Empty,
            Slug = s.Slug,
            NameEn = s.NameEn,
            NameAr = s.NameAr,
            ShortDescEn = s.ShortDescEn,
            ShortDescAr = s.ShortDescAr,
            IconName = s.IconName,
            ImagePath = s.ImagePath,
            DisplayOrder = s.DisplayOrder,
            IsPublished = s.IsPublished,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };

        private static readonly Expression<Func<CmsService, CmsServiceDetailDto>> ToDetail = s => new CmsServiceDetailDto
        {
            Id = s.Id,
            BrandId = s.BrandId,
            BrandNameEn = s.Brand != null ? s.Brand.NameEn : string.Empty,
            BrandNameAr = s.Brand != null ? s.Brand.NameAr : string.Empty,
            BrandSlug = s.Brand != null ? s.Brand.Slug : string.Empty,
            Slug = s.Slug,
            NameEn = s.NameEn,
            NameAr = s.NameAr,
            ShortDescEn = s.ShortDescEn,
            ShortDescAr = s.ShortDescAr,
            DescriptionEn = s.DescriptionEn,
            DescriptionAr = s.DescriptionAr,
            IconName = s.IconName,
            ImagePath = s.ImagePath,
            DisplayOrder = s.DisplayOrder,
            IsPublished = s.IsPublished,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };

        public CmsServiceService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<CmsServiceListItemDto>> SearchAsync(CmsServiceQuery query)
        {
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var dbQuery = _db.CmsServices
                .AsNoTracking()
                .Include(s => s.Brand)
                .AsQueryable();

            if (query.BrandId.HasValue && query.BrandId.Value > 0)
            {
                dbQuery = dbQuery.Where(s => s.BrandId == query.BrandId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(query.BrandSlug))
            {
                var normalizedBrandSlug = query.BrandSlug.Trim().ToLowerInvariant();
                dbQuery = dbQuery.Where(s => s.Brand != null && s.Brand.Slug == normalizedBrandSlug);
            }

            if (query.IsPublished.HasValue)
            {
                dbQuery = dbQuery.Where(s => s.IsPublished == query.IsPublished.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var term = query.Q.Trim();
                dbQuery = dbQuery.Where(s =>
                    s.NameEn.Contains(term) ||
                    s.NameAr.Contains(term) ||
                    (s.ShortDescEn != null && s.ShortDescEn.Contains(term)) ||
                    (s.ShortDescAr != null && s.ShortDescAr.Contains(term)));
            }

            var totalCount = await dbQuery.CountAsync();

            var items = await dbQuery
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.NameEn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToListItem)
                .ToListAsync();

            return new PagedResult<CmsServiceListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<IReadOnlyList<CmsServiceListItemDto>> GetByBrandAsync(int brandId, bool onlyPublished = false)
        {
            var query = _db.CmsServices
                .AsNoTracking()
                .Include(s => s.Brand)
                .Where(s => s.BrandId == brandId);

            if (onlyPublished)
            {
                query = query.Where(s => s.IsPublished);
            }

            return await query
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.NameEn)
                .Select(ToListItem)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<CmsServiceListItemDto>> GetByBrandSlugAsync(string brandSlug, bool onlyPublished = false)
        {
            if (string.IsNullOrWhiteSpace(brandSlug)) return Array.Empty<CmsServiceListItemDto>();

            var normalizedSlug = brandSlug.Trim().ToLowerInvariant();
            var query = _db.CmsServices
                .AsNoTracking()
                .Include(s => s.Brand)
                .Where(s => s.Brand != null && s.Brand.Slug == normalizedSlug);

            if (onlyPublished)
            {
                query = query.Where(s => s.IsPublished);
            }

            return await query
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.NameEn)
                .Select(ToListItem)
                .ToListAsync();
        }

        public async Task<CmsServiceDetailDto?> GetByIdAsync(int id)
        {
            return await _db.CmsServices
                .AsNoTracking()
                .Include(s => s.Brand)
                .Where(s => s.Id == id)
                .Select(ToDetail)
                .FirstOrDefaultAsync();
        }

        public async Task<CmsServiceDetailDto?> GetBySlugAsync(int brandId, string slug, bool onlyPublished = false)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;

            var normalizedSlug = slug.Trim().ToLowerInvariant();
            var query = _db.CmsServices
                .AsNoTracking()
                .Include(s => s.Brand)
                .Where(s => s.BrandId == brandId && s.Slug == normalizedSlug);

            if (onlyPublished)
            {
                query = query.Where(s => s.IsPublished);
            }

            return await query.Select(ToDetail).FirstOrDefaultAsync();
        }

        public async Task<ServiceResult<CmsServiceDetailDto>> CreateAsync(CmsServiceInput input)
        {
            var errors = await ValidateAsync(input);
            if (errors.Count > 0)
            {
                return ServiceResult<CmsServiceDetailDto>.Invalid(errors.ToArray());
            }

            var slug = await GenerateUniqueSlugAsync(input.BrandId, input.Slug, input.NameEn, null);

            var entity = new CmsService
            {
                BrandId = input.BrandId,
                Slug = slug,
                NameEn = input.NameEn.Trim(),
                NameAr = input.NameAr.Trim(),
                ShortDescEn = Text.Clean(input.ShortDescEn),
                ShortDescAr = Text.Clean(input.ShortDescAr),
                DescriptionEn = Text.Clean(input.DescriptionEn),
                DescriptionAr = Text.Clean(input.DescriptionAr),
                IconName = Text.Clean(input.IconName) ?? "auto_awesome",
                ImagePath = Text.Clean(input.ImagePath),
                DisplayOrder = input.DisplayOrder,
                IsPublished = input.IsPublished,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.CmsServices.Add(entity);
            await _db.SaveChangesAsync();

            var detail = await GetByIdAsync(entity.Id);
            return ServiceResult<CmsServiceDetailDto>.Success(detail!);
        }

        public async Task<ServiceResult<CmsServiceDetailDto>> UpdateAsync(int id, CmsServiceInput input)
        {
            var entity = await _db.CmsServices.FirstOrDefaultAsync(s => s.Id == id);
            if (entity is null)
            {
                return ServiceResult<CmsServiceDetailDto>.Missing();
            }

            var errors = await ValidateAsync(input);
            if (errors.Count > 0)
            {
                return ServiceResult<CmsServiceDetailDto>.Invalid(errors.ToArray());
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
            entity.IconName = Text.Clean(input.IconName) ?? "auto_awesome";
            entity.ImagePath = Text.Clean(input.ImagePath);
            entity.DisplayOrder = input.DisplayOrder;
            entity.IsPublished = input.IsPublished;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var detail = await GetByIdAsync(entity.Id);
            return ServiceResult<CmsServiceDetailDto>.Success(detail!);
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var entity = await _db.CmsServices.FirstOrDefaultAsync(s => s.Id == id);
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
            var entity = await _db.CmsServices.FirstOrDefaultAsync(s => s.Id == id);
            if (entity is null)
            {
                return ServiceResult<bool>.Missing();
            }

            entity.IsPublished = !entity.IsPublished;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ServiceResult<bool>.Success(entity.IsPublished);
        }

        public async Task SeedDefaultServicesIfEmptyAsync()
        {
            if (await _db.CmsServices.AnyAsync())
            {
                return;
            }

            var daleelBrand = await _db.BrandProfiles.FirstOrDefaultAsync(b => b.Slug == "daleel");
            var neurixBrand = await _db.BrandProfiles.FirstOrDefaultAsync(b => b.Slug == "neurix");

            if (daleelBrand == null) return;

            var services = new List<CmsService>
            {
                // Daleel Services
                new()
                {
                    BrandId = daleelBrand.Id,
                    Slug = "enterprise-intelligence",
                    NameEn = "Enterprise Intelligence & Analytics",
                    NameAr = "ذكاء الأعمال والتحليلات المؤسسية",
                    ShortDescEn = "Automated data ingestion, deep business insights, and predictive forecasting engines.",
                    ShortDescAr = "استيعاب مؤتمت للبيانات، واستخراج رؤى استراتيجية عميقة ومحركات تنبؤ متقدمة للأعمال.",
                    DescriptionEn = "Comprehensive enterprise analytics combining real-time business telemetry, regulatory compliance monitoring, and dynamic executive dashboards.",
                    DescriptionAr = "منظومة تحليلات شاملة تجمع بين قياس الأداء الفوري، ومراقبة الامتثال واللوائح، ولوحات قيادة تنفيذية تفاعلية.",
                    IconName = "insights",
                    DisplayOrder = 1,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    BrandId = daleelBrand.Id,
                    Slug = "workflow-automation",
                    NameEn = "Intelligent Workflow Automation",
                    NameAr = "أتمتة العمليات وسير العمل الذكي",
                    ShortDescEn = "Zero-friction pipeline orchestration and enterprise process automation.",
                    ShortDescAr = "إدارة مسارات العمل بسلاسة فائقة وأتمتة العمليات المؤسسية المعقدة دون تعقيد.",
                    DescriptionEn = "End-to-end task automation, CRM synchronization, and multi-department approval routing powered by intelligent AI triggers.",
                    DescriptionAr = "أتمتة شاملة للمهام، ومزامنة بيانات العملاء، وتوجيه مسارات الموافقة بين الأقسام بالاعتماد على مشغلات الذكاء الاصطناعي.",
                    IconName = "account_tree",
                    DisplayOrder = 2,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    BrandId = daleelBrand.Id,
                    Slug = "crm-pipeline-management",
                    NameEn = "CRM & Deal Pipeline Engine",
                    NameAr = "محرك إدارة علاقات العملاء والصفقات",
                    ShortDescEn = "Unified lead tracking, opportunity scoring, and client lifecycle management.",
                    ShortDescAr = "تتبع موحد للعملاء المحتملين، وتقييم الفرص البيعية، وإدارة دورة حياة العميل بالكامل.",
                    DescriptionEn = "Full 360-degree customer relationship management designed for high-touch enterprise sales cycles in Saudi Arabia.",
                    DescriptionAr = "إدارة شاملة ومتكاملة لعلاقات العملاء صُممت خصيصاً لدورات المبيعات والصفقات المؤسسية في السوق السعودي.",
                    IconName = "hub",
                    DisplayOrder = 3,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            if (neurixBrand != null)
            {
                services.AddRange(new[]
                {
                    new CmsService
                    {
                        BrandId = neurixBrand.Id,
                        Slug = "neural-predictive-models",
                        NameEn = "Neural Predictive Analytics",
                        NameAr = "نماذج التحليلات العصبية التنبؤية",
                        ShortDescEn = "State-of-the-art deep learning architectures for enterprise time-series forecasting.",
                        ShortDescAr = "هندسة تعلم عميق متطورة للتنبؤ بالسلاسل الزمنية والاتجاهات المستقبلية للمؤسسات.",
                        DescriptionEn = "High-accuracy neural network models trained on multi-modal market data to forecast risk, supply chain fluctuations, and market demands.",
                        DescriptionAr = "نماذج شبكات عصبية فائقة الدقة مدربة على بيانات متعددة الوسائط للتنبؤ بالمخاطر وتقلبات سلاسل الإمداد والطلب في الأسواق.",
                        IconName = "psychology",
                        DisplayOrder = 1,
                        IsPublished = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new CmsService
                    {
                        BrandId = neurixBrand.Id,
                        Slug = "autonomous-agent-systems",
                        NameEn = "Autonomous AI Agent Systems",
                        NameAr = "أنظمة وكلاء الذكاء الاصطناعي المستقلة",
                        ShortDescEn = "Multi-agent systems designed to perform complex cognitive tasks autonomously.",
                        ShortDescAr = "منظومة وكلاء متعددة صُممت لتنفيذ المهام الإدراكية والتحليلية المعقدة باستقلالية كاملة.",
                        DescriptionEn = "Autonomous agent swarms that coordinate research, data aggregation, and decision validation for institutional operations.",
                        DescriptionAr = "أسراب وكلاء ذكاء اصطناعي مستقلة تنسق البحث وتجميع البيانات والتحقق من القرارات للعمليات المؤسسية الكبرى.",
                        IconName = "smart_toy",
                        DisplayOrder = 2,
                        IsPublished = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                });
            }

            await _db.CmsServices.AddRangeAsync(services);
            await _db.SaveChangesAsync();
        }

        private async Task<List<ServiceError>> ValidateAsync(CmsServiceInput input)
        {
            var errors = new List<ServiceError>();

            if (input.BrandId <= 0 || !await _db.BrandProfiles.AnyAsync(b => b.Id == input.BrandId))
            {
                errors.Add(new ServiceError(nameof(input.BrandId), "Select a valid brand profile."));
            }

            if (string.IsNullOrWhiteSpace(input.NameEn))
            {
                errors.Add(new ServiceError(nameof(input.NameEn), "English service name is required."));
            }

            if (string.IsNullOrWhiteSpace(input.NameAr))
            {
                errors.Add(new ServiceError(nameof(input.NameAr), "Arabic service name is required."));
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
                baseSlug = "service-" + Guid.NewGuid().ToString("N")[..8];
            }

            var slug = baseSlug;
            var counter = 1;

            while (true)
            {
                var query = _db.CmsServices.AsNoTracking().Where(s => s.BrandId == brandId && s.Slug == slug);
                if (currentId.HasValue)
                {
                    query = query.Where(s => s.Id != currentId.Value);
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
