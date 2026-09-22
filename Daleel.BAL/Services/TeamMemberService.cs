using System.Linq.Expressions;
using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daleel.BAL.Services
{
    public class TeamMemberService : ITeamMemberService
    {
        private readonly ApplicationDbContext _db;

        private static readonly Expression<Func<TeamMember, TeamMemberListItemDto>> ToListItem = t => new TeamMemberListItemDto
        {
            Id = t.Id,
            BrandId = t.BrandId,
            BrandNameEn = t.Brand != null ? t.Brand.NameEn : string.Empty,
            BrandNameAr = t.Brand != null ? t.Brand.NameAr : string.Empty,
            BrandSlug = t.Brand != null ? t.Brand.Slug : string.Empty,
            Slug = t.Slug,
            NameEn = t.NameEn,
            NameAr = t.NameAr,
            TitleEn = t.TitleEn,
            TitleAr = t.TitleAr,
            BioEn = t.BioEn,
            BioAr = t.BioAr,
            PhotoPath = t.PhotoPath,
            Email = t.Email,
            LinkedInUrl = t.LinkedInUrl,
            DisplayOrder = t.DisplayOrder,
            IsPublished = t.IsPublished,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

        private static readonly Expression<Func<TeamMember, TeamMemberDetailDto>> ToDetail = t => new TeamMemberDetailDto
        {
            Id = t.Id,
            BrandId = t.BrandId,
            BrandNameEn = t.Brand != null ? t.Brand.NameEn : string.Empty,
            BrandNameAr = t.Brand != null ? t.Brand.NameAr : string.Empty,
            BrandSlug = t.Brand != null ? t.Brand.Slug : string.Empty,
            Slug = t.Slug,
            NameEn = t.NameEn,
            NameAr = t.NameAr,
            TitleEn = t.TitleEn,
            TitleAr = t.TitleAr,
            BioEn = t.BioEn,
            BioAr = t.BioAr,
            PhotoPath = t.PhotoPath,
            Email = t.Email,
            LinkedInUrl = t.LinkedInUrl,
            DisplayOrder = t.DisplayOrder,
            IsPublished = t.IsPublished,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

        public TeamMemberService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<TeamMemberListItemDto>> SearchAsync(TeamMemberQuery query)
        {
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var dbQuery = _db.TeamMembers
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
                    t.NameEn.Contains(term) ||
                    t.NameAr.Contains(term) ||
                    (t.TitleEn != null && t.TitleEn.Contains(term)) ||
                    (t.TitleAr != null && t.TitleAr.Contains(term)));
            }

            var totalCount = await dbQuery.CountAsync();

            var items = await dbQuery
                .OrderBy(t => t.DisplayOrder)
                .ThenBy(t => t.NameEn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToListItem)
                .ToListAsync();

            return new PagedResult<TeamMemberListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<IReadOnlyList<TeamMemberListItemDto>> GetByBrandAsync(int brandId, bool onlyPublished = false)
        {
            var query = _db.TeamMembers
                .AsNoTracking()
                .Include(t => t.Brand)
                .Where(t => t.BrandId == brandId);

            if (onlyPublished)
            {
                query = query.Where(t => t.IsPublished);
            }

            return await query
                .OrderBy(t => t.DisplayOrder)
                .ThenBy(t => t.NameEn)
                .Select(ToListItem)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<TeamMemberListItemDto>> GetByBrandSlugAsync(string brandSlug, bool onlyPublished = false)
        {
            if (string.IsNullOrWhiteSpace(brandSlug)) return Array.Empty<TeamMemberListItemDto>();

            var normalizedSlug = brandSlug.Trim().ToLowerInvariant();
            var query = _db.TeamMembers
                .AsNoTracking()
                .Include(t => t.Brand)
                .Where(t => t.Brand != null && t.Brand.Slug == normalizedSlug);

            if (onlyPublished)
            {
                query = query.Where(t => t.IsPublished);
            }

            return await query
                .OrderBy(t => t.DisplayOrder)
                .ThenBy(t => t.NameEn)
                .Select(ToListItem)
                .ToListAsync();
        }

        public async Task<TeamMemberDetailDto?> GetByIdAsync(int id)
        {
            return await _db.TeamMembers
                .AsNoTracking()
                .Include(t => t.Brand)
                .Where(t => t.Id == id)
                .Select(ToDetail)
                .FirstOrDefaultAsync();
        }

        public async Task<TeamMemberDetailDto?> GetBySlugAsync(int brandId, string slug, bool onlyPublished = false)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;

            var normalizedSlug = slug.Trim().ToLowerInvariant();
            var query = _db.TeamMembers
                .AsNoTracking()
                .Include(t => t.Brand)
                .Where(t => t.BrandId == brandId && t.Slug == normalizedSlug);

            if (onlyPublished)
            {
                query = query.Where(t => t.IsPublished);
            }

            return await query.Select(ToDetail).FirstOrDefaultAsync();
        }

        public async Task<ServiceResult<TeamMemberDetailDto>> CreateAsync(TeamMemberInput input)
        {
            var errors = await ValidateAsync(input);
            if (errors.Count > 0)
            {
                return ServiceResult<TeamMemberDetailDto>.Invalid(errors.ToArray());
            }

            var slug = await GenerateUniqueSlugAsync(input.BrandId, input.Slug, input.NameEn, null);

            var entity = new TeamMember
            {
                BrandId = input.BrandId,
                Slug = slug,
                NameEn = input.NameEn.Trim(),
                NameAr = input.NameAr.Trim(),
                TitleEn = Text.Clean(input.TitleEn),
                TitleAr = Text.Clean(input.TitleAr),
                BioEn = Text.Clean(input.BioEn),
                BioAr = Text.Clean(input.BioAr),
                PhotoPath = Text.Clean(input.PhotoPath),
                Email = Text.Clean(input.Email),
                LinkedInUrl = Text.Clean(input.LinkedInUrl),
                DisplayOrder = input.DisplayOrder,
                IsPublished = input.IsPublished,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.TeamMembers.Add(entity);
            await _db.SaveChangesAsync();

            var detail = await GetByIdAsync(entity.Id);
            return ServiceResult<TeamMemberDetailDto>.Success(detail!);
        }

        public async Task<ServiceResult<TeamMemberDetailDto>> UpdateAsync(int id, TeamMemberInput input)
        {
            var entity = await _db.TeamMembers.FirstOrDefaultAsync(t => t.Id == id);
            if (entity is null)
            {
                return ServiceResult<TeamMemberDetailDto>.Missing();
            }

            var errors = await ValidateAsync(input);
            if (errors.Count > 0)
            {
                return ServiceResult<TeamMemberDetailDto>.Invalid(errors.ToArray());
            }

            var slug = await GenerateUniqueSlugAsync(input.BrandId, input.Slug, input.NameEn, id);

            entity.BrandId = input.BrandId;
            entity.Slug = slug;
            entity.NameEn = input.NameEn.Trim();
            entity.NameAr = input.NameAr.Trim();
            entity.TitleEn = Text.Clean(input.TitleEn);
            entity.TitleAr = Text.Clean(input.TitleAr);
            entity.BioEn = Text.Clean(input.BioEn);
            entity.BioAr = Text.Clean(input.BioAr);
            entity.Email = Text.Clean(input.Email);
            entity.LinkedInUrl = Text.Clean(input.LinkedInUrl);
            entity.PhotoPath = Text.Clean(input.PhotoPath);
            entity.DisplayOrder = input.DisplayOrder;
            entity.IsPublished = input.IsPublished;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var detail = await GetByIdAsync(entity.Id);
            return ServiceResult<TeamMemberDetailDto>.Success(detail!);
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            var entity = await _db.TeamMembers.FirstOrDefaultAsync(t => t.Id == id);
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
            var entity = await _db.TeamMembers.FirstOrDefaultAsync(t => t.Id == id);
            if (entity is null)
            {
                return ServiceResult<bool>.Missing();
            }

            entity.IsPublished = !entity.IsPublished;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ServiceResult<bool>.Success(entity.IsPublished);
        }

        public async Task SeedDefaultTeamIfEmptyAsync()
        {
            if (await _db.TeamMembers.AnyAsync())
            {
                return;
            }

            var daleelBrand = await _db.BrandProfiles.FirstOrDefaultAsync(b => b.Slug == "daleel");
            if (daleelBrand == null) return;

            var members = new List<TeamMember>
            {
                new()
                {
                    BrandId = daleelBrand.Id,
                    Slug = "ahmed-al-rashid",
                    NameEn = "Ahmed Al-Rashid",
                    NameAr = "أحمد الراشد",
                    TitleEn = "Chief Executive Officer",
                    TitleAr = "الرئيس التنفيذي",
                    BioEn = "Visionary technology leader with 15+ years driving enterprise AI transformation across the GCC region.",
                    BioAr = "قائد تقني ذو رؤية استراتيجية بخبرة تفوق 15 عاماً في التحول الرقمي والذكاء الاصطناعي المؤسسي في منطقة الخليج.",
                    Email = "ahmed@aidaleel.com",
                    LinkedInUrl = "https://linkedin.com/in/",
                    DisplayOrder = 1,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    BrandId = daleelBrand.Id,
                    Slug = "sara-al-otaibi",
                    NameEn = "Sara Al-Otaibi",
                    NameAr = "سارة العتيبي",
                    TitleEn = "Chief Technology Officer",
                    TitleAr = "مديرة التقنية",
                    BioEn = "Full-stack architect specializing in scalable cloud-native platforms and neural network deployment pipelines.",
                    BioAr = "مهندسة معمارية متكاملة متخصصة في المنصات السحابية القابلة للتوسع وخطوط نشر الشبكات العصبية.",
                    Email = "sara@aidaleel.com",
                    LinkedInUrl = "https://linkedin.com/in/",
                    DisplayOrder = 2,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    BrandId = daleelBrand.Id,
                    Slug = "khalid-mansour",
                    NameEn = "Khalid Mansour",
                    NameAr = "خالد منصور",
                    TitleEn = "Head of AI Research",
                    TitleAr = "رئيس أبحاث الذكاء الاصطناعي",
                    BioEn = "PhD in Machine Learning from KAUST. Leads predictive analytics and autonomous agent systems research.",
                    BioAr = "حاصل على الدكتوراه في تعلم الآلة من جامعة الملك عبدالله للعلوم والتقنية. يقود أبحاث التحليلات التنبؤية وأنظمة الوكلاء المستقلة.",
                    Email = "khalid@aidaleel.com",
                    LinkedInUrl = "https://linkedin.com/in/",
                    DisplayOrder = 3,
                    IsPublished = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await _db.TeamMembers.AddRangeAsync(members);
            await _db.SaveChangesAsync();
        }

        private async Task<List<ServiceError>> ValidateAsync(TeamMemberInput input)
        {
            var errors = new List<ServiceError>();

            if (input.BrandId <= 0 || !await _db.BrandProfiles.AnyAsync(b => b.Id == input.BrandId))
            {
                errors.Add(new ServiceError(nameof(input.BrandId), "Select a valid brand profile."));
            }

            if (string.IsNullOrWhiteSpace(input.NameEn))
            {
                errors.Add(new ServiceError(nameof(input.NameEn), "English name is required."));
            }

            if (string.IsNullOrWhiteSpace(input.NameAr))
            {
                errors.Add(new ServiceError(nameof(input.NameAr), "Arabic name is required."));
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
                baseSlug = "member-" + Guid.NewGuid().ToString("N")[..8];
            }

            var slug = baseSlug;
            var counter = 1;

            while (true)
            {
                var query = _db.TeamMembers.AsNoTracking().Where(t => t.BrandId == brandId && t.Slug == slug);
                if (currentId.HasValue)
                {
                    query = query.Where(t => t.Id != currentId.Value);
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
