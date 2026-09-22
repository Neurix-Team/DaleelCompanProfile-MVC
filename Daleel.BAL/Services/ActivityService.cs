using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daleel.BAL.Services
{
    /// <inheritdoc />
    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _db;

        public ActivityService(ApplicationDbContext db) => _db = db;

        public async Task<IReadOnlyList<ActivityListItem>> GetForEntityAsync(CrmEntityType entityType, int entityId) =>
            await _db.Activities
                .AsNoTracking()
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.OccurredAt)
                .ThenByDescending(a => a.Id)
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

        public async Task<ServiceResult<Activity>> CreateAsync(ActivityInput input, string? createdByUserId)
        {
            if (string.IsNullOrWhiteSpace(input.Subject))
            {
                return ServiceResult<Activity>.Invalid(nameof(input.Subject), "Subject is required.");
            }

            // The (type, id) link has no foreign key behind it, so the record it points
            // at may have been deleted while the form was open.
            if (!await CrmEntityLookup.ExistsAsync(_db, input.EntityType, input.EntityId))
            {
                return ServiceResult<Activity>.Invalid(
                    nameof(input.EntityId), "That record no longer exists.");
            }

            var occurredAt = input.OccurredAt ?? DateTime.UtcNow;
            if (occurredAt > DateTime.UtcNow.AddDays(1))
            {
                return ServiceResult<Activity>.Invalid(
                    nameof(input.OccurredAt), "An activity cannot be logged in the future.");
            }

            var activity = new Activity
            {
                EntityType = input.EntityType,
                EntityId = input.EntityId,
                Type = input.Type,
                Subject = input.Subject.Trim(),
                Notes = Text.Clean(input.Notes),
                OccurredAt = occurredAt,
                CreatedByUserId = string.IsNullOrWhiteSpace(createdByUserId) ? null : createdByUserId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Activities.Add(activity);
            await _db.SaveChangesAsync();

            return ServiceResult<Activity>.Success(activity);
        }

        public async Task<ServiceResult<Activity>> DeleteAsync(int id)
        {
            var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == id);
            if (activity is null)
            {
                return ServiceResult<Activity>.Missing();
            }

            _db.Activities.Remove(activity);
            await _db.SaveChangesAsync();

            return ServiceResult<Activity>.Success(activity);
        }
    }
}
