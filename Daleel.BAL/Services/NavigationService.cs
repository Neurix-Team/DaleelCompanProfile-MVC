using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Daleel.BAL.Services
{
    public class NavigationService : INavigationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache? _cache;
        private const string CacheKeyHeader = "Nav_Active_Header_Links";
        private const string CacheKeyFooter = "Nav_Active_Footer_Links";

        public NavigationService(ApplicationDbContext db, IMemoryCache? cache = null)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<IReadOnlyList<NavigationLinkDto>> GetAllAsync(NavigationLinkQuery? query = null)
        {
            var q = _db.NavigationLinks.AsNoTracking().AsQueryable();

            if (query != null)
            {
                if (query.Location.HasValue)
                {
                    q = q.Where(n => n.Location == query.Location.Value);
                }

                if (query.Section.HasValue)
                {
                    q = q.Where(n => n.Section == query.Section.Value);
                }

                if (query.IsActive.HasValue)
                {
                    q = q.Where(n => n.IsActive == query.IsActive.Value);
                }

                if (!string.IsNullOrWhiteSpace(query.Q))
                {
                    var term = query.Q.Trim().ToLower();
                    q = q.Where(n => n.LabelEn.ToLower().Contains(term) ||
                                     n.LabelAr.ToLower().Contains(term) ||
                                     n.Url.ToLower().Contains(term));
                }
            }

            var links = await q.OrderBy(n => n.Location)
                               .ThenBy(n => n.Section)
                               .ThenBy(n => n.SortOrder)
                               .ThenBy(n => n.Id)
                               .ToListAsync();

            return links.Select(MapToDto).ToList();
        }

        public async Task<NavigationLinkDto?> GetByIdAsync(int id)
        {
            var entity = await _db.NavigationLinks.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<ServiceResult<NavigationLinkDto>> CreateAsync(NavigationLinkInput input)
        {
            var errors = ValidateInput(input);
            if (errors.Count > 0)
            {
                return ServiceResult<NavigationLinkDto>.Invalid(errors.ToArray());
            }

            // If sort order is 0, auto-assign next sort order in group
            var sortOrder = input.SortOrder;
            if (sortOrder <= 0)
            {
                var maxSort = await _db.NavigationLinks
                    .Where(n => n.Location == input.Location && n.Section == input.Section)
                    .Select(n => (int?)n.SortOrder)
                    .MaxAsync() ?? 0;
                sortOrder = maxSort + 1;
            }

            var entity = new NavigationLink
            {
                Location = input.Location,
                Section = input.Location == NavigationLocation.Footer ? input.Section : null,
                LabelEn = input.LabelEn.Trim(),
                LabelAr = input.LabelAr.Trim(),
                Url = input.Url.Trim(),
                SortOrder = sortOrder,
                IsActive = input.IsActive,
                OpenInNewTab = input.OpenInNewTab,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.NavigationLinks.Add(entity);
            await _db.SaveChangesAsync();

            InvalidateCache();
            return ServiceResult<NavigationLinkDto>.Success(MapToDto(entity));
        }

        public async Task<ServiceResult<NavigationLinkDto>> UpdateAsync(int id, NavigationLinkInput input)
        {
            var errors = ValidateInput(input);
            if (errors.Count > 0)
            {
                return ServiceResult<NavigationLinkDto>.Invalid(errors.ToArray());
            }

            var entity = await _db.NavigationLinks.FindAsync(id);
            if (entity == null)
            {
                return ServiceResult<NavigationLinkDto>.Missing();
            }

            entity.Location = input.Location;
            entity.Section = input.Location == NavigationLocation.Footer ? input.Section : null;
            entity.LabelEn = input.LabelEn.Trim();
            entity.LabelAr = input.LabelAr.Trim();
            entity.Url = input.Url.Trim();
            entity.SortOrder = input.SortOrder;
            entity.IsActive = input.IsActive;
            entity.OpenInNewTab = input.OpenInNewTab;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            InvalidateCache();
            return ServiceResult<NavigationLinkDto>.Success(MapToDto(entity));
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int id)
        {
            var entity = await _db.NavigationLinks.FindAsync(id);
            if (entity == null)
            {
                return ServiceResult<bool>.Missing();
            }

            _db.NavigationLinks.Remove(entity);
            await _db.SaveChangesAsync();

            InvalidateCache();
            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> ToggleActiveAsync(int id)
        {
            var entity = await _db.NavigationLinks.FindAsync(id);
            if (entity == null)
            {
                return ServiceResult<bool>.Missing();
            }

            entity.IsActive = !entity.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            InvalidateCache();
            return ServiceResult<bool>.Success(entity.IsActive);
        }

        public async Task<ServiceResult<bool>> UpdateSortOrderAsync(int id, int newSortOrder)
        {
            var entity = await _db.NavigationLinks.FindAsync(id);
            if (entity == null)
            {
                return ServiceResult<bool>.Missing();
            }

            entity.SortOrder = newSortOrder;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            InvalidateCache();
            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> ReorderAsync(IReadOnlyList<int> orderedIds)
        {
            if (orderedIds == null || orderedIds.Count == 0)
            {
                return ServiceResult<bool>.Success(true);
            }

            var links = await _db.NavigationLinks.Where(n => orderedIds.Contains(n.Id)).ToListAsync();
            for (int i = 0; i < orderedIds.Count; i++)
            {
                var id = orderedIds[i];
                var link = links.FirstOrDefault(n => n.Id == id);
                if (link != null)
                {
                    link.SortOrder = i + 1;
                    link.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
            InvalidateCache();
            return ServiceResult<bool>.Success(true);
        }

        public async Task<IReadOnlyList<NavigationLinkDto>> GetActiveHeaderLinksAsync()
        {
            if (_cache != null && _cache.TryGetValue(CacheKeyHeader, out IReadOnlyList<NavigationLinkDto>? cached) && cached != null)
            {
                return cached;
            }

            var links = await _db.NavigationLinks
                .AsNoTracking()
                .Where(n => n.Location == NavigationLocation.Header && n.IsActive)
                .OrderBy(n => n.SortOrder)
                .ThenBy(n => n.Id)
                .ToListAsync();

            var dtos = links.Select(MapToDto).ToList();

            _cache?.Set(CacheKeyHeader, dtos, TimeSpan.FromMinutes(10));
            return dtos;
        }

        public async Task<IReadOnlyDictionary<FooterSection, IReadOnlyList<NavigationLinkDto>>> GetActiveFooterLinksGroupedAsync()
        {
            if (_cache != null && _cache.TryGetValue(CacheKeyFooter, out IReadOnlyDictionary<FooterSection, IReadOnlyList<NavigationLinkDto>>? cached) && cached != null)
            {
                return cached;
            }

            var links = await _db.NavigationLinks
                .AsNoTracking()
                .Where(n => n.Location == NavigationLocation.Footer && n.IsActive)
                .OrderBy(n => n.Section)
                .ThenBy(n => n.SortOrder)
                .ThenBy(n => n.Id)
                .ToListAsync();

            var dtos = links.Select(MapToDto).ToList();

            var grouped = new Dictionary<FooterSection, IReadOnlyList<NavigationLinkDto>>
            {
                [FooterSection.Platform] = dtos.Where(d => d.Section == FooterSection.Platform).ToList(),
                [FooterSection.Company] = dtos.Where(d => d.Section == FooterSection.Company).ToList(),
                [FooterSection.StayInformed] = dtos.Where(d => d.Section == FooterSection.StayInformed).ToList()
            };

            _cache?.Set(CacheKeyFooter, grouped, TimeSpan.FromMinutes(10));
            return grouped;
        }

        public async Task<GroupedNavigationDto> GetGroupedNavigationAsync()
        {
            var all = await GetAllAsync();

            return new GroupedNavigationDto
            {
                HeaderLinks = all.Where(n => n.Location == NavigationLocation.Header).OrderBy(n => n.SortOrder).ToList(),
                FooterPlatformLinks = all.Where(n => n.Location == NavigationLocation.Footer && n.Section == FooterSection.Platform).OrderBy(n => n.SortOrder).ToList(),
                FooterCompanyLinks = all.Where(n => n.Location == NavigationLocation.Footer && n.Section == FooterSection.Company).OrderBy(n => n.SortOrder).ToList(),
                FooterStayInformedLinks = all.Where(n => n.Location == NavigationLocation.Footer && n.Section == FooterSection.StayInformed).OrderBy(n => n.SortOrder).ToList()
            };
        }

        public async Task SeedDefaultLinksIfEmptyAsync()
        {
            if (await _db.NavigationLinks.AnyAsync())
            {
                return;
            }

            var defaults = new List<NavigationLink>
            {
                // Header links (7 items)
                new() { Location = NavigationLocation.Header, Section = null, LabelEn = "Home", LabelAr = "الرئيسية", Url = "/", SortOrder = 1, IsActive = true },
                new() { Location = NavigationLocation.Header, Section = null, LabelEn = "Platforms", LabelAr = "المنصات", Url = "/Home/Platforms", SortOrder = 2, IsActive = true },
                new() { Location = NavigationLocation.Header, Section = null, LabelEn = "Shop", LabelAr = "المتجر", Url = "/Home/Shop", SortOrder = 3, IsActive = true },
                new() { Location = NavigationLocation.Header, Section = null, LabelEn = "Trust & Governance", LabelAr = "الثقة والحوكمة", Url = "/Home/Trust", SortOrder = 4, IsActive = true },
                new() { Location = NavigationLocation.Header, Section = null, LabelEn = "About Us", LabelAr = "من نحن", Url = "/Home/About", SortOrder = 5, IsActive = true },
                new() { Location = NavigationLocation.Header, Section = null, LabelEn = "Insights & News", LabelAr = "الأخبار والرؤى", Url = "/Home/Blog", SortOrder = 6, IsActive = true },
                new() { Location = NavigationLocation.Header, Section = null, LabelEn = "Contact Us", LabelAr = "اتصل بنا", Url = "/Home/Contact", SortOrder = 7, IsActive = true },

                // Footer Platform links (5 items - inactive by default until real content is ready)
                new() { Location = NavigationLocation.Footer, Section = FooterSection.Platform, LabelEn = "Solutions", LabelAr = "الحلول", Url = "/Home/Soon", SortOrder = 1, IsActive = false },
                new() { Location = NavigationLocation.Footer, Section = FooterSection.Platform, LabelEn = "Intelligence Feed", LabelAr = "تغذية الذكاء", Url = "/Home/Soon", SortOrder = 2, IsActive = false },
                new() { Location = NavigationLocation.Footer, Section = FooterSection.Platform, LabelEn = "Risk Dashboard", LabelAr = "لوحة مؤشرات المخاطر", Url = "/Home/Soon", SortOrder = 3, IsActive = false },
                new() { Location = NavigationLocation.Footer, Section = FooterSection.Platform, LabelEn = "API Access", LabelAr = "الوصول إلى API", Url = "/Home/Soon", SortOrder = 4, IsActive = false },
                new() { Location = NavigationLocation.Footer, Section = FooterSection.Platform, LabelEn = "Integrations", LabelAr = "التكاملات", Url = "/Home/Soon", SortOrder = 5, IsActive = false },

                // Footer Company links (4 items)
                new() { Location = NavigationLocation.Footer, Section = FooterSection.Company, LabelEn = "About Us", LabelAr = "من نحن", Url = "/Home/About", SortOrder = 1, IsActive = true },
                new() { Location = NavigationLocation.Footer, Section = FooterSection.Company, LabelEn = "Blog & Insights", LabelAr = "الأخبار والمقالات", Url = "/Home/Blog", SortOrder = 2, IsActive = true },
                new() { Location = NavigationLocation.Footer, Section = FooterSection.Company, LabelEn = "Trust & Governance", LabelAr = "الثقة والحوكمة", Url = "/Home/Trust", SortOrder = 3, IsActive = true },
                new() { Location = NavigationLocation.Footer, Section = FooterSection.Company, LabelEn = "Contact Us", LabelAr = "اتصل بنا", Url = "/Home/Contact", SortOrder = 4, IsActive = true }
            };

            _db.NavigationLinks.AddRange(defaults);
            await _db.SaveChangesAsync();

            InvalidateCache();
        }

        private void InvalidateCache()
        {
            _cache?.Remove(CacheKeyHeader);
            _cache?.Remove(CacheKeyFooter);
        }

        private static List<ServiceError> ValidateInput(NavigationLinkInput input)
        {
            var errors = new List<ServiceError>();

            if (string.IsNullOrWhiteSpace(input.LabelEn))
            {
                errors.Add(new ServiceError(nameof(input.LabelEn), "English label is required."));
            }

            if (string.IsNullOrWhiteSpace(input.LabelAr))
            {
                errors.Add(new ServiceError(nameof(input.LabelAr), "Arabic label is required."));
            }

            if (string.IsNullOrWhiteSpace(input.Url))
            {
                errors.Add(new ServiceError(nameof(input.Url), "URL / Path is required."));
            }

            if (input.Location == NavigationLocation.Footer && !input.Section.HasValue)
            {
                errors.Add(new ServiceError(nameof(input.Section), "Footer column / section is required for footer links."));
            }

            return errors;
        }

        private static NavigationLinkDto MapToDto(NavigationLink entity)
        {
            return new NavigationLinkDto
            {
                Id = entity.Id,
                Location = entity.Location,
                Section = entity.Section,
                LabelEn = entity.LabelEn,
                LabelAr = entity.LabelAr,
                Url = entity.Url,
                SortOrder = entity.SortOrder,
                IsActive = entity.IsActive,
                OpenInNewTab = entity.OpenInNewTab,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
