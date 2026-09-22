using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daleel.BAL.Services
{
    /// <inheritdoc />
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _db;

        public DashboardService(ApplicationDbContext db) => _db = db;

        public async Task<DashboardKpis> GetKpisAsync()
        {
            var today = DateTime.UtcNow.Date;

            // Archived leads are excluded everywhere so the headline count matches what
            // /crm/leads shows by default.
            var leads = _db.Leads.AsNoTracking().Where(l => !l.IsArchived);
            var deals = _db.Deals.AsNoTracking();
            var tasks = _db.CrmTasks.AsNoTracking();

            return new DashboardKpis
            {
                TotalLeads = await leads.CountAsync(),
                NewLeads = await leads.CountAsync(l => l.Status == LeadStatus.New),
                QualifiedLeads = await leads.CountAsync(l => l.Status == LeadStatus.Qualified),
                ConvertedLeads = await leads.CountAsync(l => l.Status == LeadStatus.Converted),

                TotalCustomers = await _db.Contacts.AsNoTracking()
                    .CountAsync(c => c.Status == RecordStatus.Active),
                TotalCompanies = await _db.Companies.AsNoTracking()
                    .CountAsync(c => c.Status == RecordStatus.Active),

                OpenDeals = await deals.CountAsync(d => d.Stage != DealStage.Won && d.Stage != DealStage.Lost),
                WonDeals = await deals.CountAsync(d => d.Stage == DealStage.Won),
                LostDeals = await deals.CountAsync(d => d.Stage == DealStage.Lost),

                OpenTasks = await tasks.CountAsync(t => t.Status != CrmTaskStatus.Done),
                OverdueTasks = await tasks.CountAsync(t =>
                    t.Status != CrmTaskStatus.Done && t.DueDate != null && t.DueDate < today)
            };
        }

        public async Task<IReadOnlyList<CurrencyTotal>> GetPipelineByCurrencyAsync()
        {
            // Grouped into an anonymous type: EF cannot translate a record constructor
            // inside a GroupBy projection, so the record is built after materialising.
            var rows = await _db.Deals
                .AsNoTracking()
                .Where(d => d.Stage != DealStage.Won && d.Stage != DealStage.Lost)
                .GroupBy(d => d.Currency)
                .Select(g => new { Currency = g.Key, Total = g.Sum(d => d.Value), Count = g.Count() })
                .ToListAsync();

            return rows
                .OrderByDescending(r => r.Total)
                .Select(r => new CurrencyTotal(r.Currency, r.Total, r.Count))
                .ToList();
        }

        public async Task<IReadOnlyList<TaskListItem>> GetUpcomingTasksAsync(int take = 5)
        {
            var items = await _db.CrmTasks
                .AsNoTracking()
                .Where(t => t.Status != CrmTaskStatus.Done)
                .OrderBy(t => t.DueDate == null)
                .ThenBy(t => t.DueDate)
                .Take(Math.Max(1, take))
                .Select(TaskService.Project)
                .ToListAsync();

            await CrmEntityLookup.LabelAsync(_db, items);

            return items;
        }

        public async Task<IReadOnlyList<ActivityListItem>> GetRecentActivitiesAsync(int take = 5) =>
            await _db.Activities
                .AsNoTracking()
                .OrderByDescending(a => a.OccurredAt)
                .ThenByDescending(a => a.Id)
                .Take(Math.Max(1, take))
                .Select(a => new ActivityListItem
                {
                    Id = a.Id,
                    Type = a.Type,
                    Subject = a.Subject,
                    Notes = a.Notes,
                    OccurredAt = a.OccurredAt,
                    CreatedByName = a.CreatedBy != null
                        ? a.CreatedBy.FirstName + " " + a.CreatedBy.LastName
                        : null,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId
                })
                .ToListAsync();
    }
}
