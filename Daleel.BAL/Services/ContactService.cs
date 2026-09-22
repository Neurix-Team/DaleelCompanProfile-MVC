using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daleel.BAL.Services
{
    /// <inheritdoc />
    public class ContactService : IContactService
    {
        private readonly ApplicationDbContext _db;

        public ContactService(ApplicationDbContext db) => _db = db;

        public async Task<PagedResult<ContactListItem>> SearchAsync(ContactQuery query)
        {
            var contacts = _db.Contacts.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var term = query.Q.Trim();
                contacts = contacts.Where(c =>
                    (c.FirstName + " " + c.LastName).Contains(term) ||
                    c.FirstName.Contains(term) ||
                    c.LastName.Contains(term) ||
                    (c.Email != null && c.Email.Contains(term)) ||
                    (c.Phone != null && c.Phone.Contains(term)) ||
                    (c.JobTitle != null && c.JobTitle.Contains(term)) ||
                    (c.Company != null && c.Company.Name.Contains(term)) ||
                    (c.Notes != null && c.Notes.Contains(term)));
            }

            if (query.Status.HasValue)
            {
                contacts = contacts.Where(c => c.Status == query.Status.Value);
            }

            if (query.CompanyId.HasValue)
            {
                contacts = contacts.Where(c => c.CompanyId == query.CompanyId.Value);
            }

            var page = Math.Max(1, query.Page);
            var pageSize = query.PageSize > 0 ? query.PageSize : PagedQuery.DefaultPageSize;
            var totalCount = await contacts.CountAsync();

            var items = await contacts
                .OrderBy(c => c.FirstName)
                .ThenBy(c => c.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ContactListItem
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    JobTitle = c.JobTitle,
                    Email = c.Email,
                    Phone = c.Phone,
                    CompanyId = c.CompanyId,
                    CompanyName = c.Company != null ? c.Company.Name : null,
                    Status = c.Status
                })
                .ToListAsync();

            return new PagedResult<ContactListItem>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public Task<Contact?> GetWithCompanyAsync(int id) =>
            _db.Contacts
                .AsNoTracking()
                .Include(c => c.Company)
                .FirstOrDefaultAsync(c => c.Id == id);

        public Task<Contact?> GetAsync(int id) =>
            _db.Contacts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

        public async Task<ServiceResult<Contact>> CreateAsync(ContactInput input)
        {
            var companyError = await ValidateCompanyAsync(input.CompanyId);
            if (companyError is not null)
            {
                return ServiceResult<Contact>.Invalid(companyError);
            }

            var now = DateTime.UtcNow;
            var contact = new Contact { CreatedAt = now, UpdatedAt = now };
            Apply(input, contact);

            _db.Contacts.Add(contact);
            await _db.SaveChangesAsync();

            return ServiceResult<Contact>.Success(contact);
        }

        public async Task<ServiceResult<Contact>> UpdateAsync(int id, ContactInput input)
        {
            var contact = await _db.Contacts.FirstOrDefaultAsync(c => c.Id == id);
            if (contact is null)
            {
                return ServiceResult<Contact>.Missing();
            }

            var companyError = await ValidateCompanyAsync(input.CompanyId);
            if (companyError is not null)
            {
                return ServiceResult<Contact>.Invalid(companyError);
            }

            Apply(input, contact);
            contact.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return ServiceResult<Contact>.Success(contact);
        }

        public async Task<ServiceResult<string>> DeleteAsync(int id)
        {
            var contact = await _db.Contacts.FirstOrDefaultAsync(c => c.Id == id);
            if (contact is null)
            {
                return ServiceResult<string>.Missing();
            }

            var name = contact.FullName;
            _db.Contacts.Remove(contact);
            await _db.SaveChangesAsync();

            return ServiceResult<string>.Success(name);
        }

        /// <summary>
        /// Guards against the company being deleted between the form being rendered
        /// and submitted, which would otherwise surface as an FK violation.
        /// </summary>
        private async Task<ServiceError?> ValidateCompanyAsync(int? companyId)
        {
            if (!companyId.HasValue)
            {
                return null;
            }

            var exists = await _db.Companies.AnyAsync(c => c.Id == companyId.Value);
            return exists
                ? null
                : new ServiceError(
                    nameof(ContactInput.CompanyId),
                    "The selected company no longer exists. Please choose another.");
        }

        private static void Apply(ContactInput input, Contact contact)
        {
            contact.FirstName = input.FirstName.Trim();
            contact.LastName = input.LastName.Trim();
            contact.JobTitle = Text.Clean(input.JobTitle);
            contact.Email = Text.Clean(input.Email);
            contact.Phone = Text.Clean(input.Phone);
            contact.CompanyId = input.CompanyId;
            contact.Status = input.Status;
            contact.Notes = Text.Clean(input.Notes);
        }
    }
}
