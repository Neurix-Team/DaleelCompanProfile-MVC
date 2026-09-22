using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Entities;
using Daleel.Models;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.ViewComponents
{
    /// <summary>
    /// The task list and quick-add for one record. See
    /// <see cref="RecordActivityViewComponent"/> for why this is a component.
    /// </summary>
    public class RecordTasksViewComponent : ViewComponent
    {
        private readonly ITaskService _tasks;

        public RecordTasksViewComponent(ITaskService tasks) => _tasks = tasks;

        public async Task<IViewComponentResult> InvokeAsync(CrmEntityType entityType, int entityId)
        {
            ViewData["Tasks"] = await _tasks.GetForEntityAsync(entityType, entityId);

            return View(new TaskFormViewModel
            {
                EntityType = entityType,
                EntityId = entityId,
                DueDate = DateTime.UtcNow.Date.AddDays(3)
            });
        }
    }
}
