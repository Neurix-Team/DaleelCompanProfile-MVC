using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Daleel.BAL.Services
{
    /// <inheritdoc />
    public class LeadService : ILeadService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<LeadService> _logger;

        public LeadService(ApplicationDbContext db, ILogger<LeadService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<PagedResult<LeadListItem>> SearchAsync(LeadQuery query)
        {
            var leads = _db.Leads.AsNoTracking();

            if (!query.ShowArchived)
            {
                leads = leads.Where(l => !l.IsArchived);
            }

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var term = query.Q.Trim();
                leads = leads.Where(l =>
                    (l.FirstName + " " + l.LastName).Contains(term) ||
                    l.FirstName.Contains(term) ||
                    l.LastName.Contains(term) ||
                    (l.JobTitle != null && l.JobTitle.Contains(term)) ||
                    (l.Email != null && l.Email.Contains(term)) ||
                    (l.Phone != null && l.Phone.Contains(term)) ||
                    (l.CompanyName != null && l.CompanyName.Contains(term)) ||
                    (l.Notes != null && l.Notes.Contains(term)));
            }

            if (query.Status.HasValue)
            {
                leads = leads.Where(l => l.Status == query.Status.Value);
            }

            if (query.Source.HasValue)
            {
                leads = leads.Where(l => l.Source == query.Source.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.AssignedToId))
            {
                leads = leads.Where(l => l.AssignedToId == query.AssignedToId);
            }

            var page = Math.Max(1, query.Page);
            var pageSize = query.PageSize > 0 ? query.PageSize : PagedQuery.DefaultPageSize;
            var totalCount = await leads.CountAsync();

            var items = await leads
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new LeadListItem
                {
                    Id = l.Id,
                    FirstName = l.FirstName,
                    LastName = l.LastName,
                    Email = l.Email,
                    Phone = l.Phone,
                    CompanyName = l.CompanyName,
                    Status = l.Status,
                    Source = l.Source,
                    AssignedToName = l.AssignedTo != null
                        ? l.AssignedTo.FirstName + " " + l.AssignedTo.LastName
                        : null,
                    IsArchived = l.IsArchived,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<LeadListItem>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public Task<Lead?> GetWithRelationsAsync(int id) =>
            _db.Leads
                .AsNoTracking()
                .Include(l => l.AssignedTo)
                .Include(l => l.ConvertedContact)
                .FirstOrDefaultAsync(l => l.Id == id);

        public Task<Lead?> GetAsync(int id) =>
            _db.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);

        public async Task<ServiceResult<Lead>> CreateAsync(LeadInput input)
        {
            var assigneeError = await ValidateAssigneeAsync(input.AssignedToId);
            if (assigneeError is not null)
            {
                return ServiceResult<Lead>.Invalid(assigneeError);
            }

            var now = DateTime.UtcNow;
            var lead = new Lead { CreatedAt = now, UpdatedAt = now };
            Apply(input, lead);

            _db.Leads.Add(lead);
            await _db.SaveChangesAsync();

            return ServiceResult<Lead>.Success(lead);
        }

        public async Task<ServiceResult<Lead>> UpdateAsync(int id, LeadInput input)
        {
            var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == id);
            if (lead is null)
            {
                return ServiceResult<Lead>.Missing();
            }

            var assigneeError = await ValidateAssigneeAsync(input.AssignedToId);
            if (assigneeError is not null)
            {
                return ServiceResult<Lead>.Invalid(assigneeError);
            }

            Apply(input, lead);
            lead.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return ServiceResult<Lead>.Success(lead);
        }

        public async Task<ServiceResult<Lead>> SetArchivedAsync(int id, bool archived)
        {
            var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == id);
            if (lead is null)
            {
                return ServiceResult<Lead>.Missing();
            }

            lead.IsArchived = archived;
            lead.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return ServiceResult<Lead>.Success(lead);
        }

        public async Task<bool> CaptureAsync(LeadCaptureInput input)
        {
            var now = DateTime.UtcNow;
            var lead = new Lead
            {
                FirstName = input.FirstName.Trim(),
                LastName = input.LastName.Trim(),
                Email = Text.Clean(input.Email),
                CompanyName = Text.Clean(input.CompanyName),
                JobTitle = Text.Clean(input.JobTitle),
                Status = LeadStatus.New,
                Source = input.Source,
                Notes = Text.Clean(input.Notes),
                CreatedAt = now,
                UpdatedAt = now
            };

            try
            {
                _db.Leads.Add(lead);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to capture a {Source} lead for {Email}.", input.Source, input.Email);
                return false;
            }
        }

        public async Task<ServiceResult<LeadConversionContext>> GetConversionContextAsync(int id)
        {
            var lead = await GetAsync(id);
            if (lead is null)
            {
                return ServiceResult<LeadConversionContext>.Missing();
            }

            var blocked = BlockedReason(lead);
            if (blocked is not null)
            {
                return ServiceResult<LeadConversionContext>.Invalid(string.Empty, blocked);
            }

            var duplicate = await FindDuplicateContactAsync(lead.Email);
            var companyName = Text.Clean(lead.CompanyName);
            var matchedCompanyId = companyName is null
                ? null
                : await FindCompanyIdByNameAsync(companyName);

            return ServiceResult<LeadConversionContext>.Success(new LeadConversionContext(
                LeadId: lead.Id,
                LeadName: lead.FullName,
                FirstName: lead.FirstName,
                LastName: lead.LastName,
                Email: lead.Email,
                Phone: lead.Phone,
                JobTitle: lead.JobTitle,
                DuplicateContact: duplicate,
                MatchedCompanyId: matchedCompanyId,
                UnmatchedCompanyName: matchedCompanyId is null ? companyName : null));
        }

        public async Task<ServiceResult<LeadConversionResult>> ConvertAsync(int id, LeadConversionInput input)
        {
            var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == id);
            if (lead is null)
            {
                return ServiceResult<LeadConversionResult>.Missing();
            }

            // Re-checked here, not only when the form was built: the lead may have been
            // converted or archived in another tab while this form sat open.
            var blocked = BlockedReason(lead);
            if (blocked is not null)
            {
                return ServiceResult<LeadConversionResult>.Invalid(string.Empty, blocked);
            }

            var now = DateTime.UtcNow;
            Contact contact;
            var linkedExisting = false;

            if (input.LinkExistingContact)
            {
                if (!input.ExistingContactId.HasValue)
                {
                    return ServiceResult<LeadConversionResult>.Invalid(
                        nameof(LeadConversionInput.ExistingContactId),
                        "No existing contact was selected to link to.");
                }

                var existing = await _db.Contacts
                    .FirstOrDefaultAsync(c => c.Id == input.ExistingContactId.Value);

                if (existing is null)
                {
                    return ServiceResult<LeadConversionResult>.Invalid(
                        nameof(LeadConversionInput.ExistingContactId),
                        "That contact no longer exists. Uncheck the box to create a new contact instead.");
                }

                // Linking, not merging — the existing record's details and company stay as
                // they are. Overwriting them from a lead would silently lose curated data.
                contact = existing;
                linkedExisting = true;
            }
            else
            {
                var companyError = await ValidateConversionCompanyAsync(input);
                if (companyError is not null)
                {
                    return ServiceResult<LeadConversionResult>.Invalid(companyError);
                }

                contact = new Contact
                {
                    FirstName = input.FirstName.Trim(),
                    LastName = input.LastName.Trim(),
                    Email = Text.Clean(input.Email),
                    Phone = Text.Clean(input.Phone),
                    JobTitle = Text.Clean(input.JobTitle),
                    Status = RecordStatus.Active,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                if (input.CreateNewCompany)
                {
                    // Navigation property rather than an id, so the company and contact
                    // insert together in a single transaction.
                    contact.Company = new Company
                    {
                        Name = input.NewCompanyName!.Trim(),
                        Status = RecordStatus.Active,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                }
                else if (input.ExistingCompanyId.HasValue)
                {
                    contact.CompanyId = input.ExistingCompanyId.Value;
                }

                _db.Contacts.Add(contact);
            }

            lead.Status = LeadStatus.Converted;
            lead.ConvertedContact = contact;
            lead.ConvertedAt = now;
            lead.UpdatedAt = now;

            await _db.SaveChangesAsync();

            return ServiceResult<LeadConversionResult>.Success(new LeadConversionResult(
                ContactId: contact.Id,
                ContactName: contact.FullName,
                LeadName: lead.FullName,
                LinkedExisting: linkedExisting));
        }

        public async Task<IReadOnlyList<ListOption>> GetAssignableUsersAsync() =>
            await _db.Users
                .AsNoTracking()
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new ListOption(u.Id, u.FirstName + " " + u.LastName))
                .ToListAsync();

        /// <summary>Why this lead cannot be converted right now, or null if it can be.</summary>
        private static string? BlockedReason(Lead lead)
        {
            if (lead.Status == LeadStatus.Converted)
            {
                return $"Lead \"{lead.FullName}\" has already been converted.";
            }

            if (lead.IsArchived)
            {
                return $"Lead \"{lead.FullName}\" is archived. Restore it before converting.";
            }

            return null;
        }

        /// <summary>An existing contact already using this email address, if there is one.</summary>
        private async Task<ContactListItem?> FindDuplicateContactAsync(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var term = email.Trim();

            return await _db.Contacts
                .AsNoTracking()
                .Where(c => c.Email == term)
                .OrderBy(c => c.Id)
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
                .FirstOrDefaultAsync();
        }

        private async Task<int?> FindCompanyIdByNameAsync(string name) =>
            await _db.Companies
                .AsNoTracking()
                .Where(c => c.Name == name)
                .OrderBy(c => c.Id)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();

        private async Task<ServiceError?> ValidateConversionCompanyAsync(LeadConversionInput input)
        {
            if (input.CreateNewCompany)
            {
                if (string.IsNullOrWhiteSpace(input.NewCompanyName))
                {
                    return new ServiceError(
                        nameof(LeadConversionInput.NewCompanyName),
                        "Enter a name for the new company, or pick an existing one.");
                }

                var normalizedName = input.NewCompanyName.Trim().ToUpperInvariant();
                var duplicateExists = await _db.Companies
                    .AsNoTracking()
                    .AnyAsync(c => c.Name.ToUpper() == normalizedName);

                return duplicateExists
                    ? new ServiceError(
                        nameof(LeadConversionInput.NewCompanyName),
                        "A company with this name already exists. Select the existing company instead.")
                    : null;
            }

            if (!input.ExistingCompanyId.HasValue)
            {
                return null;
            }

            var exists = await _db.Companies.AnyAsync(c => c.Id == input.ExistingCompanyId.Value);
            return exists
                ? null
                : new ServiceError(
                    nameof(LeadConversionInput.ExistingCompanyId),
                    "The selected company no longer exists. Please choose another.");
        }

        /// <summary>
        /// Guards against the assigned user being removed between the form being
        /// rendered and submitted, which would otherwise be an FK violation.
        /// </summary>
        private async Task<ServiceError?> ValidateAssigneeAsync(string? assignedToId)
        {
            if (string.IsNullOrWhiteSpace(assignedToId))
            {
                return null;
            }

            var exists = await _db.Users.AnyAsync(u => u.Id == assignedToId);
            return exists
                ? null
                : new ServiceError(
                    nameof(LeadInput.AssignedToId),
                    "The selected employee no longer exists. Please choose another.");
        }

        private static void Apply(LeadInput input, Lead lead)
        {
            lead.FirstName = input.FirstName.Trim();
            lead.LastName = input.LastName.Trim();
            lead.Email = Text.Clean(input.Email);
            lead.Phone = Text.Clean(input.Phone);
            lead.CompanyName = Text.Clean(input.CompanyName);
            lead.JobTitle = Text.Clean(input.JobTitle);
            lead.Status = input.Status;
            lead.Source = input.Source;
            lead.AssignedToId = Text.Clean(input.AssignedToId);
            lead.Notes = Text.Clean(input.Notes);
        }
    }
}
