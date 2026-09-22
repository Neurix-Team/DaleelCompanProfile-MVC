using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daleel.BAL.Services
{
    /// <inheritdoc />
    public class CompanyService : ICompanyService
    {
        private readonly ApplicationDbContext _db;

        public CompanyService(ApplicationDbContext db) => _db = db;

        public async Task<PagedResult<CompanyListItem>> SearchAsync(CompanyQuery query)
        {
            var companies = _db.Companies.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var term = query.Q.Trim();
                companies = companies.Where(c =>
                    c.Name.Contains(term) ||
                    (c.Industry != null && c.Industry.Contains(term)) ||
                    (c.Email != null && c.Email.Contains(term)) ||
                    (c.Phone != null && c.Phone.Contains(term)) ||
                    (c.City != null && c.City.Contains(term)) ||
                    (c.Country != null && c.Country.Contains(term)) ||
                    (c.Description != null && c.Description.Contains(term)));
            }

            if (query.Status.HasValue)
            {
                companies = companies.Where(c => c.Status == query.Status.Value);
            }

            var page = Math.Max(1, query.Page);
            var pageSize = query.PageSize > 0 ? query.PageSize : PagedQuery.DefaultPageSize;
            var totalCount = await companies.CountAsync();

            var items = await companies
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CompanyListItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    Industry = c.Industry,
                    City = c.City,
                    Country = c.Country,
                    Email = c.Email,
                    Phone = c.Phone,
                    Status = c.Status,
                    ContactCount = c.Contacts.Count
                })
                .ToListAsync();

            return new PagedResult<CompanyListItem>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public Task<Company?> GetWithContactsAsync(int id) =>
            _db.Companies
                .AsNoTracking()
                .Include(c => c.Contacts.OrderBy(ct => ct.FirstName).ThenBy(ct => ct.LastName))
                .FirstOrDefaultAsync(c => c.Id == id);

        public Task<Company?> GetAsync(int id) =>
            _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

        public async Task<ServiceResult<Company>> CreateAsync(CompanyInput input)
        {
            var duplicateErrors = await FindDuplicateErrorsAsync(input);
            if (duplicateErrors.Count > 0)
            {
                return ServiceResult<Company>.Invalid(duplicateErrors.ToArray());
            }

            var now = DateTime.UtcNow;
            var company = new Company { CreatedAt = now, UpdatedAt = now };
            Apply(input, company);

            _db.Companies.Add(company);
            await _db.SaveChangesAsync();

            return ServiceResult<Company>.Success(company);
        }

        public async Task<ServiceResult<Company>> UpdateAsync(int id, CompanyInput input)
        {
            var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (company is null)
            {
                return ServiceResult<Company>.Missing();
            }

            var duplicateErrors = await FindDuplicateErrorsAsync(input, id);
            if (duplicateErrors.Count > 0)
            {
                return ServiceResult<Company>.Invalid(duplicateErrors.ToArray());
            }

            Apply(input, company);
            company.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return ServiceResult<Company>.Success(company);
        }

        public async Task<ServiceResult<CompanyDeletionResult>> DeleteAsync(int id)
        {
            var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (company is null)
            {
                return ServiceResult<CompanyDeletionResult>.Missing();
            }

            // Contacts are intentionally not deleted — the FK is configured to null out.
            var orphaned = await _db.Contacts.CountAsync(c => c.CompanyId == id);
            var name = company.Name;

            _db.Companies.Remove(company);
            await _db.SaveChangesAsync();

            return ServiceResult<CompanyDeletionResult>.Success(
                new CompanyDeletionResult(name, orphaned));
        }

        private async Task<IReadOnlyList<ServiceError>> FindDuplicateErrorsAsync(
            CompanyInput input,
            int? excludedCompanyId = null)
        {
            var errors = new List<ServiceError>();
            var normalizedName = input.Name.Trim().ToUpperInvariant();
            var normalizedEmail = Text.Clean(input.Email)?.ToUpperInvariant();
            var companies = _db.Companies.AsNoTracking();

            if (excludedCompanyId.HasValue)
            {
                companies = companies.Where(c => c.Id != excludedCompanyId.Value);
            }

            if (await companies.AnyAsync(c => c.Name.ToUpper() == normalizedName))
            {
                errors.Add(new ServiceError(
                    nameof(CompanyInput.Name),
                    "A company with this name already exists."));
            }

            if (normalizedEmail is not null &&
                await companies.AnyAsync(c => c.Email != null && c.Email.ToUpper() == normalizedEmail))
            {
                errors.Add(new ServiceError(
                    nameof(CompanyInput.Email),
                    "A company with this email address already exists."));
            }

            return errors;
        }

        public async Task<IReadOnlyList<ListOption>> GetOptionsAsync() =>
            await _db.Companies
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new ListOption(c.Id.ToString(), c.Name))
                .ToListAsync();

        /// <summary>Copies the accepted fields onto the entity, trimming free text.</summary>
        private static void Apply(CompanyInput input, Company company)
        {
            company.Name = input.Name.Trim();
            company.Industry = Text.Clean(input.Industry);
            company.Website = Text.Clean(input.Website);
            company.Phone = Text.Clean(input.Phone);
            company.Email = Text.Clean(input.Email);
            company.Address = Text.Clean(input.Address);
            company.City = Text.Clean(input.City);
            company.Country = Text.Clean(input.Country);
            company.Description = Text.Clean(input.Description);
            company.Status = input.Status;
        }
    }
}
