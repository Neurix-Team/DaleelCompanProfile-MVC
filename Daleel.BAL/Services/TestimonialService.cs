using System.Linq.Expressions;
using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daleel.BAL.Services
{
    public class TestimonialService : ITestimonialService
    {
        private readonly ApplicationDbContext _db;

        private static readonly Expression<Func<Testimonial, TestimonialListItemDto>> ToListItem = t => new TestimonialListItemDto
        {
            Id = t.Id,
            BrandId = t.BrandId,
            BrandNameEn = t.Brand != null ? t.Brand.NameEn : string.Empty,
            BrandNameAr = t.Brand != null ? t.Brand.NameAr : string.Empty,
            BrandSlug = t.Brand != null ? t.Brand.Slug : string.Empty,
            CustomerNameEn = t.CustomerNameEn,
            CustomerNameAr = t.CustomerNameAr,
            CompanyNameEn = t.CompanyNameEn,
            CompanyNameAr = t.CompanyNameAr,
            RoleTitleEn = t.RoleTitleEn,
            RoleTitleAr = t.RoleTitleAr,
            ContentEn = t.ContentEn,
            ContentAr = t.ContentAr,
            Rating = t.Rating,
            PhotoPath = t.PhotoPath,
            DisplayOrder = t.DisplayOrder,
            IsPublished = t.IsPublished,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

        private static readonly Expression<Func<Testimonial, TestimonialDetailDto>> ToDetail = t => new TestimonialDetailDto
        {
            Id = t.Id,
            BrandId = t.BrandId,
            BrandNameEn = t.Brand != null ? t.Brand.NameEn : string.Empty,
            BrandNameAr = t.Brand != null ? t.Brand.NameAr : string.Empty,
            BrandSlug = t.Brand != null ? t.Brand.Slug : string.Empty,
            CustomerNameEn = t.CustomerNameEn,
            CustomerNameAr = t.CustomerNameAr,
            CompanyNameEn = t.CompanyNameEn,
            CompanyNameAr = t.CompanyNameAr,
            RoleTitleEn = t.RoleTitleEn,
            RoleTitleAr = t.RoleTitleAr,
            ContentEn = t.ContentEn,
            ContentAr = t.ContentAr,
            Rating = t.Rating,
            PhotoPath = t.PhotoPath,
            DisplayOrder = t.DisplayOrder,
            IsPublished = t.IsPublished,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

        public TestimonialService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<TestimonialListItemDto>> SearchAsync(TestimonialQuery query)
        {
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var dbQuery = _db.Testimonials
                .AsNoTracking()
                .Include(t => t.Brand)
                .AsQueryable();

            if (query.BrandId.HasValue && query.BrandId.Value > 0)
            {
                dbQuery = dbQuery.Where(t => t.BrandId == query.BrandId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(query.BrandSlug))
            {
                var normalizedBrandSlug = query.BrandSlug.Trim().ToLowerInvariant();
                dbQuery = dbQuery.Where(t => t.Brand != null && t.Brand.Slug == normalizedBrandSlug);
            }

            if (query.IsPublished.HasValue)
            {
                dbQuery = dbQuery.Where(t => t.IsPublished == query.IsPublished.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var term = query.Q.Trim();
                dbQuery = dbQuery.Where(t =>
                    t.CustomerNameEn.Contains(term) ||
                    t.CustomerNameAr.Contains(term) ||
                    (t.CompanyNameEn != null && t.CompanyNameEn.Contains(term)) ||
                    (t.CompanyNameAr != null && t.CompanyNameAr.Contains(term)) ||
                    (t.RoleTitleEn != null && t.RoleTitleEn.Contains(term)) ||
                    (t.RoleTitleAr != null && t.RoleTitleAr.Contains(term)) ||
                    t.ContentEn.Contains(term) ||
                    t.ContentAr.Contains(term));
            }

            var totalCount = await dbQuery.CountAsync();

            var items = await dbQuery
                .OrderBy(t => t.DisplayOrder)
                .ThenByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToListItem)
                .ToListAsync();

            return new PagedResult<TestimonialListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<IReadOnlyList<TestimonialListItemDto>> GetByBrandAsync(int brandId, bool onlyPublished = false)
        {
            var query = _db.Testimonials
                .AsNoTracking()
                .Include(t => t.Brand)
                .Where(t => t.BrandId == brandId);

            if (onlyPublished)
            {
                query = query.Where(t => t.IsPublished);
            }

            return await query
                .OrderBy(t => t.DisplayOrder)
                .ThenByDescending(t => t.CreatedAt)
                .Select(ToListItem)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<TestimonialListItemDto>> GetByBrandSlugAsync(string brandSlug, bool onlyPublished = false)
        {
            if (string.IsNullOrWhiteSpace(brandSlug)) return Array.Empty<TestimonialListItemDto>();

            var normalizedSlug = brandSlug.Trim().ToLowerInvariant();
            var query = _db.Testimonials
                .AsNoTracking()
                .Include(t => t.Brand)
                .Where(t => t.Brand != null && t.Brand.Slug == normalizedSlug);

            if (onlyPublished)
            {
                query = query.Where(t => t.IsPublished);
            }

            return await query
                .OrderBy(t => t.DisplayOrder)
                .ThenByDescending(t => t.CreatedAt)
                .Select(ToListItem)
                .ToListAsync();
        }

        public async Task<TestimonialDetailDto?> GetByIdAsync(int id)
        {
            return await _db.Testimonials
                .AsNoTracking()
                .Include(t => t.Brand)
                .Where(t => t.Id == id)
                .Select(ToDetail)
                .FirstOrDefaultAsync();
        }

        public async Task<ServiceResult<TestimonialDetailDto>> CreateAsync(TestimonialInput input)
        {
            var errors = await ValidateAsync(input);
            if (errors.Count > 0)
            {
                return ServiceResult<TestimonialDetailDto>.Invalid(errors.ToArray());
            }

            var entity = new Testimonial
            {
                BrandId = input.BrandId,
                CustomerNameEn = input.CustomerNameEn.Trim(),
                CustomerNameAr = input.CustomerNameAr.Trim(),
                CompanyNameEn = Text.Clean(input.CompanyNameEn),
                CompanyNameAr = Text.Clean(input.CompanyNameAr),
                RoleTitleEn = Text.Clean(input.RoleTitleEn),
                RoleTitleAr = Text.Clean(input.RoleTitleAr),
                ContentEn = input.ContentEn.Trim(),
                ContentAr = input.ContentAr.Trim(),
                Rating = Math.Clamp(input.Rating, 1, 5),
                PhotoPath = Text.Clean(input.PhotoPath),
                DisplayOrder = input.DisplayOrder,
                IsPublished = input.IsPublished,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Testimonials.Add(entity);
            await _db.SaveChangesAsync();

            var detail = await GetByIdAsync(entity.Id);
            return ServiceResult<TestimonialDetailDto>.Success(detail!);
        }

        public async Task<ServiceResult<TestimonialDetailDto>> UpdateAsync(int id, TestimonialInput input)
        {
            var entity = await _db.Testimonials.FirstOrDefaultAsync(t => t.Id == id);
            if (entity is null)
            {
                return ServiceResult<TestimonialDetailDto>.Missing();
            }

            var errors = await ValidateAsync(input);
            if (errors.Count > 0)
            {
                return ServiceResult<TestimonialDetailDto>.Invalid(errors.ToArray());
            }

            entity.BrandId = input.BrandId;
            entity.CustomerNameEn = input.CustomerNameEn.Trim();
            entity.CustomerNameAr = input.CustomerNameAr.Trim();
            entity.CompanyNameEn = Text.Clean(input.CompanyNameEn);
            entity.CompanyNameAr = Text.Clean(input.CompanyNameAr);
            entity.RoleTitleEn = Text.Clean(input.RoleTitleEn);
            entity.RoleTitleAr = Text.Clean(input.RoleTitleAr);
            entity.ContentEn = input.ContentEn.Trim();
            entity.ContentAr = input.ContentAr.Trim();
            entity.Rating = Math.Clamp(input.Rating, 1, 5);
            entity.PhotoPath = Text.Clean(input.PhotoPath);
            entity.DisplayOrder = input.DisplayOrder;
            entity.IsPublished = input.IsPublished;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var detail = await GetByIdAsync(entity.Id);
            return ServiceResult<TestimonialDetailDto>.Success(detail!);
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var entity = await _db.Testimonials.FirstOrDefaultAsync(t => t.Id == id);
            if (entity is null)
            {
                return ServiceResult.Missing();
            }

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ServiceResult.Success();
        }

        public async Task<ServiceResult<bool>> TogglePublishAsync(int id)
        {
            var entity = await _db.Testimonials.FirstOrDefaultAsync(t => t.Id == id);
            if (entity is null)
            {
                return ServiceResult<bool>.Missing();
            }

            entity.IsPublished = !entity.IsPublished;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ServiceResult<bool>.Success(entity.IsPublished);
        }

        public async Task SeedDefaultTestimonialsIfEmptyAsync()
        {
            if (await _db.Testimonials.AnyAsync())
            {
                return;
            }

            var daleelBrand = await _db.BrandProfiles.FirstOrDefaultAsync(b => b.Slug == "daleel");
            var neurixBrand = await _db.BrandProfiles.FirstOrDefaultAsync(b => b.Slug == "neurix");

            if (daleelBrand == null) return;

            var testimonials = new List<Testimonial>
            {
                new()
                {
                    BrandId = daleelBrand.Id,
                    CustomerNameEn = "Fahad Al-Husseini",
                    CustomerNameAr = "فهد الحسيني",
                    CompanyNameEn = "Riyadh Logistics Co.",
                    CompanyNameAr = "شركة الرياض للخدمات اللوجستية",
                    RoleTitleEn = "VP of Operations",
                    RoleTitleAr = "نائب رئيس العمليات",
                    ContentEn = "Daleel transformed our regional dispatch visibility. The real-time telemetry and pipeline visibility delivered an unprecedented 32% operational efficiency gain within 90 days.",
                    ContentAr = "أحدثت دليل نقلة نوعية في كفاءة التوزيع والعمليات الميدانية لدينا. التحليلات اللحظية ومتابعة الأداء حققت زيادة بنسبة 32% في الكفاءة التشغيلية خلال 90 يوماً.",
                    Rating = 5,
                    DisplayOrder = 1,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    BrandId = daleelBrand.Id,
                    CustomerNameEn = "Mona Al-Qahtani",
                    CustomerNameAr = "منى القحطاني",
                    CompanyNameEn = "Afaq Capital",
                    CompanyNameAr = "آفاق كابيتال",
                    RoleTitleEn = "Managing Director",
                    RoleTitleAr = "العضو المنتدب",
                    ContentEn = "The tailored pipeline architecture and automated compliance scoring gave our investment team high velocity deal closure capabilities. Outstanding technical craftsmanship.",
                    ContentAr = "منظومة الصفقات المخصصة والتقييم الآلي للامتثال منحت فريقنا الاستثماري سرعة استثنائية في إتمام الصفقات. تميز هندسي وحرفية تقنية عالية.",
                    Rating = 5,
                    DisplayOrder = 2,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            if (neurixBrand != null)
            {
                testimonials.Add(new Testimonial
                {
                    BrandId = neurixBrand.Id,
                    CustomerNameEn = "Dr. Tariq Al-Ghamdi",
                    CustomerNameAr = "د. طارق الغامدي",
                    CompanyNameEn = "Gulf Trading Systems",
                    CompanyNameAr = "أنظمة الخليج للتداول",
                    RoleTitleEn = "Chief Investment Strategist",
                    RoleTitleAr = "كبير استراتيجيي الاستثمار",
                    ContentEn = "Neurix deep learning neural models have revolutionized our predictive volatility hedging. Accuracy and processing speed exceeded global benchmarks.",
                    ContentAr = "أحدثت النماذج العصبية والتعلم العميق من نيوريكس ثورة في استراتيجيات التحوط والتنبؤ بالتقلبات السعرية. تفوقت الدقة والسرعة على المقاييس العالمية.",
                    Rating = 5,
                    DisplayOrder = 1,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _db.Testimonials.AddRangeAsync(testimonials);
            await _db.SaveChangesAsync();
        }

        private async Task<List<ServiceError>> ValidateAsync(TestimonialInput input)
        {
            var errors = new List<ServiceError>();

            if (input.BrandId <= 0 || !await _db.BrandProfiles.AnyAsync(b => b.Id == input.BrandId))
            {
                errors.Add(new ServiceError(nameof(input.BrandId), "Select a valid brand profile."));
            }

            if (string.IsNullOrWhiteSpace(input.CustomerNameEn))
            {
                errors.Add(new ServiceError(nameof(input.CustomerNameEn), "Customer name in English is required."));
            }

            if (string.IsNullOrWhiteSpace(input.CustomerNameAr))
            {
                errors.Add(new ServiceError(nameof(input.CustomerNameAr), "Customer name in Arabic is required."));
            }

            if (string.IsNullOrWhiteSpace(input.ContentEn))
            {
                errors.Add(new ServiceError(nameof(input.ContentEn), "Testimonial content in English is required."));
            }

            if (string.IsNullOrWhiteSpace(input.ContentAr))
            {
                errors.Add(new ServiceError(nameof(input.ContentAr), "Testimonial content in Arabic is required."));
            }

            if (input.Rating < 1 || input.Rating > 5)
            {
                errors.Add(new ServiceError(nameof(input.Rating), "Rating must be between 1 and 5."));
            }

            return errors;
        }
    }
}
