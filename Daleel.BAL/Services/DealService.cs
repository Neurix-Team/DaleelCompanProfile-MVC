using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daleel.BAL.Services
{
    /// <inheritdoc />
    public class DealService : IDealService
    {
        /// <summary>Currencies offered on the deal form.</summary>
        private static readonly string[] SupportedCurrencies =
            { "USD", "EUR", "GBP", "EGP", "SAR", "AED" };

        private readonly ApplicationDbContext _db;

        public DealService(ApplicationDbContext db) => _db = db;

        public IReadOnlyList<string> GetCurrencies() => SupportedCurrencies;

        public async Task<PagedResult<DealListItem>> SearchAsync(DealQuery query)
        {
            var deals = _db.Deals.AsNoTracking();

            if (query.OpenOnly)
            {
                deals = deals.Where(d => d.Stage != DealStage.Won && d.Stage != DealStage.Lost);
            }

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var term = query.Q.Trim();
                deals = deals.Where(d =>
                    d.Name.Contains(term) ||
                    (d.Company != null && d.Company.Name.Contains(term)) ||
                    (d.PrimaryContact != null &&
                        ((d.PrimaryContact.FirstName + " " + d.PrimaryContact.LastName).Contains(term) ||
                         d.PrimaryContact.FirstName.Contains(term) ||
                         d.PrimaryContact.LastName.Contains(term) ||
                         (d.PrimaryContact.Email != null && d.PrimaryContact.Email.Contains(term)))) ||
                    (d.Description != null && d.Description.Contains(term)));
            }

            if (query.Stage.HasValue)
            {
                deals = deals.Where(d => d.Stage == query.Stage.Value);
            }

            if (query.CompanyId.HasValue)
            {
                deals = deals.Where(d => d.CompanyId == query.CompanyId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.AssignedToId))
            {
                deals = deals.Where(d => d.AssignedToId == query.AssignedToId);
            }

            deals = query.Sort switch
            {
                DealSort.Name => deals.OrderBy(d => d.Name),
                DealSort.ValueHigh => deals.OrderByDescending(d => d.Value),
                DealSort.ValueLow => deals.OrderBy(d => d.Value),
                // Deals with no target date sort last rather than leading the list.
                DealSort.CloseDate => deals
                    .OrderBy(d => d.ExpectedCloseDate == null)
                    .ThenBy(d => d.ExpectedCloseDate),
                _ => deals.OrderByDescending(d => d.CreatedAt)
            };

            var page = Math.Max(1, query.Page);
            var pageSize = query.PageSize > 0 ? query.PageSize : PagedQuery.DefaultPageSize;
            var totalCount = await deals.CountAsync();

            var items = await deals
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DealListItem
                {
                    Id = d.Id,
                    Name = d.Name,
                    CompanyId = d.CompanyId,
                    CompanyName = d.Company != null ? d.Company.Name : null,
                    PrimaryContactName = d.PrimaryContact != null
                        ? d.PrimaryContact.FirstName + " " + d.PrimaryContact.LastName
                        : null,
                    Value = d.Value,
                    Currency = d.Currency,
                    Stage = d.Stage,
                    ExpectedCloseDate = d.ExpectedCloseDate,
                    AssignedToName = d.AssignedTo != null
                        ? d.AssignedTo.FirstName + " " + d.AssignedTo.LastName
                        : null,
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<DealListItem>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PipelineBoard> GetPipelineAsync(string? assignedToId = null)
        {
            var deals = _db.Deals.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(assignedToId))
            {
                deals = deals.Where(d => d.AssignedToId == assignedToId);
            }

            var rows = await deals
                .OrderBy(d => d.ExpectedCloseDate == null)
                .ThenBy(d => d.ExpectedCloseDate)
                .ThenByDescending(d => d.Value)
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    CompanyName = d.Company != null ? d.Company.Name : null,
                    d.Value,
                    d.Currency,
                    d.Stage,
                    d.ExpectedCloseDate,
                    AssignedToName = d.AssignedTo != null
                        ? d.AssignedTo.FirstName + " " + d.AssignedTo.LastName
                        : null
                })
                .ToListAsync();

            var columns = Enum.GetValues<DealStage>()
                .Select(stage => new PipelineColumn(
                    stage,
                    rows.Where(r => r.Stage == stage)
                        .Select(r => new PipelineCard(
                            r.Id, r.Name, r.CompanyName, r.Value, r.Currency,
                            r.ExpectedCloseDate, r.AssignedToName))
                        .ToList()))
                .ToList();

            // Summing across different currencies would be meaningless, so say so instead.
            var distinctCurrencies = rows.Select(r => r.Currency).Distinct().ToList();

            return new PipelineBoard
            {
                Columns = columns,
                Currency = distinctCurrencies.Count == 1 ? distinctCurrencies[0] : null,
                IsMixedCurrency = distinctCurrencies.Count > 1
            };
        }

        public Task<Deal?> GetWithRelationsAsync(int id) =>
            _db.Deals
                .AsNoTracking()
                .Include(d => d.Company)
                .Include(d => d.PrimaryContact)
                .Include(d => d.AssignedTo)
                .FirstOrDefaultAsync(d => d.Id == id);

        public Task<Deal?> GetAsync(int id) =>
            _db.Deals.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);

        public async Task<ServiceResult<Deal>> CreateAsync(DealInput input)
        {
            var errors = await ValidateAsync(input);
            if (errors.Count > 0)
            {
                return ServiceResult<Deal>.Invalid(errors.ToArray());
            }

            var now = DateTime.UtcNow;
            var deal = new Deal { CreatedAt = now, UpdatedAt = now };
            Apply(input, deal);

            _db.Deals.Add(deal);
            await _db.SaveChangesAsync();

            return ServiceResult<Deal>.Success(deal);
        }

        public async Task<ServiceResult<Deal>> UpdateAsync(int id, DealInput input)
        {
            var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == id);
            if (deal is null)
            {
                return ServiceResult<Deal>.Missing();
            }

            var errors = await ValidateAsync(input);
            if (errors.Count > 0)
            {
                return ServiceResult<Deal>.Invalid(errors.ToArray());
            }

            Apply(input, deal);
            deal.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return ServiceResult<Deal>.Success(deal);
        }

        public async Task<ServiceResult<Deal>> MoveToStageAsync(int id, DealStage stage)
        {
            var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == id);
            if (deal is null)
            {
                return ServiceResult<Deal>.Missing();
            }

            if (deal.Stage == stage)
            {
                return ServiceResult<Deal>.Success(deal);
            }

            deal.Stage = stage;
            deal.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return ServiceResult<Deal>.Success(deal);
        }

        public async Task<ServiceResult<string>> DeleteAsync(int id)
        {
            var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == id);
            if (deal is null)
            {
                return ServiceResult<string>.Missing();
            }

            var name = deal.Name;

            _db.Deals.Remove(deal);
            await _db.SaveChangesAsync();

            return ServiceResult<string>.Success(name);
        }

        public async Task<IReadOnlyList<ListOption>> GetContactOptionsAsync(int? companyId = null)
        {
            var contacts = _db.Contacts.AsNoTracking();

            if (companyId.HasValue)
            {
                contacts = contacts.Where(c => c.CompanyId == companyId.Value);
            }

            return await contacts
                .OrderBy(c => c.FirstName)
                .ThenBy(c => c.LastName)
                .Select(c => new ListOption(
                    c.Id.ToString(),
                    c.Company != null
                        ? c.FirstName + " " + c.LastName + " (" + c.Company.Name + ")"
                        : c.FirstName + " " + c.LastName))
                .ToListAsync();
        }

        /// <summary>
        /// Rules the form cannot express: negative money, an unsupported currency, and
        /// related records that may have been deleted while the form was open.
        /// </summary>
        private async Task<List<ServiceError>> ValidateAsync(DealInput input)
        {
            var errors = new List<ServiceError>();

            if (input.Value < 0)
            {
                errors.Add(new ServiceError(nameof(input.Value), "Value cannot be negative."));
            }

            var currency = (input.Currency ?? string.Empty).Trim().ToUpperInvariant();
            if (!SupportedCurrencies.Contains(currency))
            {
                errors.Add(new ServiceError(nameof(input.Currency), "Please choose a supported currency."));
            }

            if (input.CompanyId.HasValue &&
                !await _db.Companies.AnyAsync(c => c.Id == input.CompanyId.Value))
            {
                errors.Add(new ServiceError(
                    nameof(input.CompanyId),
                    "The selected company no longer exists. Please choose another."));
            }

            if (input.PrimaryContactId.HasValue &&
                !await _db.Contacts.AnyAsync(c => c.Id == input.PrimaryContactId.Value))
            {
                errors.Add(new ServiceError(
                    nameof(input.PrimaryContactId),
                    "The selected contact no longer exists. Please choose another."));
            }

            if (!string.IsNullOrWhiteSpace(input.AssignedToId) &&
                !await _db.Users.AnyAsync(u => u.Id == input.AssignedToId))
            {
                errors.Add(new ServiceError(
                    nameof(input.AssignedToId),
                    "The selected employee no longer exists. Please choose another."));
            }

            return errors;
        }

        private static void Apply(DealInput input, Deal deal)
        {
            deal.Name = input.Name.Trim();
            deal.CompanyId = input.CompanyId;
            deal.PrimaryContactId = input.PrimaryContactId;
            deal.Value = input.Value;
            deal.Currency = input.Currency.Trim().ToUpperInvariant();
            deal.Stage = input.Stage;
            deal.ExpectedCloseDate = input.ExpectedCloseDate;
            deal.AssignedToId = string.IsNullOrWhiteSpace(input.AssignedToId) ? null : input.AssignedToId;
            deal.Description = Text.Clean(input.Description);
        }
    }
}
