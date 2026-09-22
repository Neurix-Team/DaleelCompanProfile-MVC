using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Entities;
using Daleel.Models;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.ViewComponents
{
    /// <summary>
    /// The activity log for one record. A view component rather than a partial so the
    /// four detail pages that show it do not each have to fetch the data themselves.
    /// </summary>
    public class RecordActivityViewComponent : ViewComponent
    {
        private readonly IActivityService _activities;

        public RecordActivityViewComponent(IActivityService activities) => _activities = activities;

        public async Task<IViewComponentResult> InvokeAsync(CrmEntityType entityType, int entityId)
        {
            ViewData["Activities"] = await _activities.GetForEntityAsync(entityType, entityId);

            return View(new ActivityFormViewModel
            {
                EntityType = entityType,
                EntityId = entityId
            });
        }
    }
}
