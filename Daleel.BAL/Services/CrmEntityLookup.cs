using Daleel.BAL.Models;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daleel.BAL.Services
{
    /// <summary>
    /// Resolves the polymorphic (EntityType, EntityId) link used by activities and tasks.
    /// Because that link cannot be a real foreign key, existence and display names are
    /// looked up here instead — one place, so both services stay in step.
    /// </summary>
    internal static class CrmEntityLookup
    {
        public static Task<bool> ExistsAsync(ApplicationDbContext db, CrmEntityType type, int id) => type switch
        {
            CrmEntityType.Lead => db.Leads.AnyAsync(x => x.Id == id),
            CrmEntityType.Contact => db.Contacts.AnyAsync(x => x.Id == id),
            CrmEntityType.Company => db.Companies.AnyAsync(x => x.Id == id),
            CrmEntityType.Deal => db.Deals.AnyAsync(x => x.Id == id),
            _ => Task.FromResult(false)
        };

        /// <summary>Display names for a batch of ids of one type, keyed by id.</summary>
        public static async Task<Dictionary<int, string>> GetLabelsAsync(
            ApplicationDbContext db, CrmEntityType type, IReadOnlyCollection<int> ids)
        {
            if (ids.Count == 0)
            {
                return new Dictionary<int, string>();
            }

            return type switch
            {
                CrmEntityType.Lead => await db.Leads.AsNoTracking()
                    .Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.FirstName + " " + x.LastName),

                CrmEntityType.Contact => await db.Contacts.AsNoTracking()
                    .Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.FirstName + " " + x.LastName),

                CrmEntityType.Company => await db.Companies.AsNoTracking()
                    .Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.Name),

                CrmEntityType.Deal => await db.Deals.AsNoTracking()
                    .Where(x => ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.Name),

                _ => new Dictionary<int, string>()
            };
        }

        public static async Task<IReadOnlyList<ListOption>> GetOptionsAsync(
            ApplicationDbContext db, CrmEntityType type) => type switch
        {
            CrmEntityType.Lead => await db.Leads.AsNoTracking()
                .Where(x => !x.IsArchived)
                .OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
                .Select(x => new ListOption(x.Id.ToString(), x.FirstName + " " + x.LastName))
                .ToListAsync(),

            CrmEntityType.Contact => await db.Contacts.AsNoTracking()
                .OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
                .Select(x => new ListOption(x.Id.ToString(), x.FirstName + " " + x.LastName))
                .ToListAsync(),

            CrmEntityType.Company => await db.Companies.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new ListOption(x.Id.ToString(), x.Name))
                .ToListAsync(),

            CrmEntityType.Deal => await db.Deals.AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ListOption(x.Id.ToString(), x.Name))
                .ToListAsync(),

            _ => Array.Empty<ListOption>()
        };

        /// <summary>
        /// Fills in <see cref="TaskListItem.EntityLabel"/> for a page of tasks using one
        /// query per entity type present, rather than one per row.
        /// </summary>
        public static async Task LabelAsync(ApplicationDbContext db, IReadOnlyList<TaskListItem> items)
        {
            foreach (var group in items
                .Where(i => i.EntityType.HasValue && i.EntityId.HasValue)
                .GroupBy(i => i.EntityType!.Value))
            {
                var ids = group.Select(i => i.EntityId!.Value).Distinct().ToList();
                var labels = await GetLabelsAsync(db, group.Key, ids);

                foreach (var item in group)
                {
                    item.EntityLabel = labels.TryGetValue(item.EntityId!.Value, out var label) ? label : null;
                }
            }
        }
    }
}
