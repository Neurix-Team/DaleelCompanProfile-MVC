using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Entities;
using Daleel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Daleel.Controllers
{
    /// <summary>
    /// Follow-up tasks. All data access goes through <see cref="ITaskService"/>.
    /// </summary>
    [Route("crm/tasks")]
    [Authorize]
    public class CrmTaskController : Controller
    {
        private readonly ITaskService _tasks;
        private readonly ILeadService _leads;

        public CrmTaskController(ITaskService tasks, ILeadService leads)
        {
            _tasks = tasks;
            _leads = leads;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            string? q,
            CrmTaskStatus? status,
            string? assignedToId,
            bool overdueOnly = false,
            int page = 1)
        {
            var result = await _tasks.SearchAsync(new TaskQuery
            {
                Q = q,
                Status = status,
                AssignedToId = assignedToId,
                OverdueOnly = overdueOnly,
                Page = page
            });

            ViewData["Q"] = q;
            ViewData["Status"] = status;
            ViewData["AssignedToId"] = assignedToId;
            ViewData["OverdueOnly"] = overdueOnly;
            ViewData["AssignedToOptions"] = await BuildUserOptionsAsync();

            return View(result);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create(CrmEntityType? entityType, int? entityId)
        {
            var model = new TaskFormViewModel
            {
                EntityType = entityType,
                EntityId = entityId,
                DueDate = DateTime.UtcNow.Date.AddDays(3)
            };

            await PopulateOptionsAsync(model);
            return View(model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _tasks.CreateAsync(ToInput(model));
                if (result.Succeeded)
                {
                    TempData["Success"] = $"Task \"{result.Value!.Title}\" was created.";
                    return BackOrIndex(model.ReturnUrl);
                }

                AddErrors(result.Errors);
            }

            // A failed quick-add from a record page has no form of its own to return to,
            // so it falls through to the full form rather than losing what was typed.
            await PopulateOptionsAsync(model);
            return View(model);
        }

        [HttpGet("{id:int}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _tasks.GetAsync(id);
            if (task is null)
            {
                return NotFound();
            }

            ViewData["TaskTitle"] = task.Title;

            var model = new TaskFormViewModel
            {
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                Priority = task.Priority,
                Status = task.Status,
                AssignedToId = task.AssignedToId,
                EntityType = task.EntityType,
                EntityId = task.EntityId
            };

            await PopulateOptionsAsync(model);
            return View(model);
        }

        [HttpPost("{id:int}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaskFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _tasks.UpdateAsync(id, ToInput(model));
                if (result.NotFound)
                {
                    return NotFound();
                }

                if (result.Succeeded)
                {
                    TempData["Success"] = $"Task \"{result.Value!.Title}\" was updated.";
                    return RedirectToAction(nameof(Index));
                }

                AddErrors(result.Errors);
            }

            var existing = await _tasks.GetAsync(id);
            if (existing is null)
            {
                return NotFound();
            }

            ViewData["TaskTitle"] = existing.Title;
            await PopulateOptionsAsync(model);
            return View(model);
        }

        /// <summary>One-click completion from the task list or a record's task widget.</summary>
        [HttpPost("{id:int}/complete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? returnUrl)
        {
            var result = await _tasks.SetStatusAsync(id, CrmTaskStatus.Done);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData["Success"] = $"Task \"{result.Value!.Title}\" marked done.";
            return BackOrIndex(returnUrl);
        }

        /// <summary>Puts a completed task back into play.</summary>
        [HttpPost("{id:int}/reopen")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reopen(int id, string? returnUrl)
        {
            var result = await _tasks.SetStatusAsync(id, CrmTaskStatus.Open);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData["Success"] = $"Task \"{result.Value!.Title}\" reopened.";
            return BackOrIndex(returnUrl);
        }

        [HttpPost("{id:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl)
        {
            var result = await _tasks.DeleteAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData["Success"] = $"Task \"{result.Value}\" was deleted.";
            return BackOrIndex(returnUrl);
        }

        private IActionResult BackOrIndex(string? returnUrl) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction(nameof(Index));

        private async Task PopulateOptionsAsync(TaskFormViewModel model)
        {
            model.AssignedToOptions = await BuildUserOptionsAsync();

            var options = new Dictionary<CrmEntityType, IEnumerable<SelectListItem>>();
            foreach (var type in Enum.GetValues<CrmEntityType>())
            {
                options[type] = (await _tasks.GetEntityOptionsAsync(type))
                    .Select(o => new SelectListItem { Value = o.Value, Text = o.Text })
                    .ToList();
            }

            model.EntityOptions = options;
        }

        private async Task<IEnumerable<SelectListItem>> BuildUserOptionsAsync() =>
            (await _leads.GetAssignableUsersAsync())
                .Select(o => new SelectListItem { Value = o.Value, Text = o.Text })
                .ToList();

        private static TaskInput ToInput(TaskFormViewModel model) => new()
        {
            EntityType = model.EntityType,
            EntityId = model.EntityId,
            Title = model.Title,
            Description = model.Description,
            DueDate = model.DueDate,
            Priority = model.Priority,
            Status = model.Status,
            AssignedToId = model.AssignedToId
        };

        private void AddErrors(IReadOnlyList<ServiceError> errors)
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }
        }
    }
}
