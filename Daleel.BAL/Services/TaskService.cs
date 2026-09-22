using System.Linq.Expressions;
using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Data;
using Daleel.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daleel.BAL.Services
{
    /// <inheritdoc />
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _db;

        public TaskService(ApplicationDbContext db) => _db = db;

        public async Task<PagedResult<TaskListItem>> SearchAsync(TaskQuery query)
        {
            var tasks = _db.CrmTasks.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var term = query.Q.Trim();
                tasks = tasks.Where(t => t.Title.Contains(term) ||
                                         (t.Description != null && t.Description.Contains(term)));
            }

            if (query.Status.HasValue)
            {
                tasks = tasks.Where(t => t.Status == query.Status.Value);
            }
            else if (query.OpenOnly)
            {
                tasks = tasks.Where(t => t.Status != CrmTaskStatus.Done);
            }

            if (!string.IsNullOrWhiteSpace(query.AssignedToId))
            {
                tasks = tasks.Where(t => t.AssignedToId == query.AssignedToId);
            }

            if (query.OverdueOnly)
            {
                var today = DateTime.UtcNow.Date;
                tasks = tasks.Where(t => t.Status != CrmTaskStatus.Done &&
                                         t.DueDate != null && t.DueDate < today);
            }

            // Unfinished work first, then soonest due; undated tasks sort last rather
            // than leading the list.
            tasks = tasks
                .OrderBy(t => t.Status == CrmTaskStatus.Done)
                .ThenBy(t => t.DueDate == null)
                .ThenBy(t => t.DueDate)
                .ThenByDescending(t => t.Id);

            var page = Math.Max(1, query.Page);
            var pageSize = query.PageSize > 0 ? query.PageSize : PagedQuery.DefaultPageSize;
            var totalCount = await tasks.CountAsync();

            var items = await tasks
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(Project)
                .ToListAsync();

            await CrmEntityLookup.LabelAsync(_db, items);

            return new PagedResult<TaskListItem>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<IReadOnlyList<TaskListItem>> GetForEntityAsync(CrmEntityType entityType, int entityId) =>
            await _db.CrmTasks
                .AsNoTracking()
                .Where(t => t.EntityType == entityType && t.EntityId == entityId)
                .OrderBy(t => t.Status == CrmTaskStatus.Done)
                .ThenBy(t => t.DueDate == null)
                .ThenBy(t => t.DueDate)
                .Select(Project)
                .ToListAsync();

        public Task<CrmTask?> GetAsync(int id) =>
            _db.CrmTasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

        public async Task<ServiceResult<CrmTask>> CreateAsync(TaskInput input)
        {
            var errors = await ValidateAsync(input);
            if (errors.Count > 0)
            {
                return ServiceResult<CrmTask>.Invalid(errors.ToArray());
            }

            var now = DateTime.UtcNow;
            var task = new CrmTask { CreatedAt = now, UpdatedAt = now };
            Apply(input, task);

            _db.CrmTasks.Add(task);
            await _db.SaveChangesAsync();

            return ServiceResult<CrmTask>.Success(task);
        }

        public async Task<ServiceResult<CrmTask>> UpdateAsync(int id, TaskInput input)
        {
            var task = await _db.CrmTasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task is null)
            {
                return ServiceResult<CrmTask>.Missing();
            }

            var errors = await ValidateAsync(input);
            if (errors.Count > 0)
            {
                return ServiceResult<CrmTask>.Invalid(errors.ToArray());
            }

            Apply(input, task);
            task.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return ServiceResult<CrmTask>.Success(task);
        }

        public async Task<ServiceResult<CrmTask>> SetStatusAsync(int id, CrmTaskStatus status)
        {
            var task = await _db.CrmTasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task is null)
            {
                return ServiceResult<CrmTask>.Missing();
            }

            if (task.Status != status)
            {
                task.Status = status;
                task.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return ServiceResult<CrmTask>.Success(task);
        }

        public async Task<ServiceResult<string>> DeleteAsync(int id)
        {
            var task = await _db.CrmTasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task is null)
            {
                return ServiceResult<string>.Missing();
            }

            var title = task.Title;

            _db.CrmTasks.Remove(task);
            await _db.SaveChangesAsync();

            return ServiceResult<string>.Success(title);
        }

        public Task<IReadOnlyList<ListOption>> GetEntityOptionsAsync(CrmEntityType entityType) =>
            CrmEntityLookup.GetOptionsAsync(_db, entityType);

        /// <summary>
        /// Rules the form cannot express: a half-filled record link, and related records
        /// deleted while the form was open.
        /// </summary>
        private async Task<List<ServiceError>> ValidateAsync(TaskInput input)
        {
            var errors = new List<ServiceError>();

            if (string.IsNullOrWhiteSpace(input.Title))
            {
                errors.Add(new ServiceError(nameof(input.Title), "Title is required."));
            }

            if (input.EntityType.HasValue != input.EntityId.HasValue)
            {
                errors.Add(new ServiceError(
                    nameof(input.EntityId),
                    "Please choose which record this task belongs to, or leave it unlinked."));
            }
            else if (input.EntityType.HasValue &&
                     !await CrmEntityLookup.ExistsAsync(_db, input.EntityType.Value, input.EntityId!.Value))
            {
                errors.Add(new ServiceError(
                    nameof(input.EntityId),
                    "The selected record no longer exists. Please choose another."));
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

        private static void Apply(TaskInput input, CrmTask task)
        {
            task.EntityType = input.EntityType;
            task.EntityId = input.EntityId;
            task.Title = input.Title.Trim();
            task.Description = Text.Clean(input.Description);
            task.DueDate = input.DueDate;
            task.Priority = input.Priority;
            task.Status = input.Status;
            task.AssignedToId = string.IsNullOrWhiteSpace(input.AssignedToId) ? null : input.AssignedToId;
        }

        /// <summary>
        /// Shared projection so list and widget rows always carry the same fields. Kept
        /// as an expression rather than a method so EF can translate it into SQL.
        /// </summary>
        internal static readonly Expression<Func<CrmTask, TaskListItem>> Project = t => new TaskListItem
        {
            Id = t.Id,
            Title = t.Title,
            DueDate = t.DueDate,
            Priority = t.Priority,
            Status = t.Status,
            AssignedToName = t.AssignedTo != null
                ? t.AssignedTo.FirstName + " " + t.AssignedTo.LastName
                : null,
            EntityType = t.EntityType,
            EntityId = t.EntityId
        };
    }
}
