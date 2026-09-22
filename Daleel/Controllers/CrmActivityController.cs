using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Entities;
using Daleel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Controllers
{
    /// <summary>
    /// Logging interactions against a record. Deliberately has no list page — the log
    /// lives inline on each record's detail page, which is where it is useful.
    /// </summary>
    [Route("crm/activities")]
    [Authorize]
    public class CrmActivityController : Controller
    {
        private readonly IActivityService _activities;
        private readonly UserManager<ApplicationUser> _users;

        public CrmActivityController(IActivityService activities, UserManager<ApplicationUser> users)
        {
            _activities = activities;
            _users = users;
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActivityFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // The inline form has nowhere of its own to re-render into, so the first
                // problem is carried back to the detail page as a flash message.
                TempData["Error"] = FirstError() ?? "The activity could not be logged.";
                return Back(model.ReturnUrl, model.EntityType, model.EntityId);
            }

            var result = await _activities.CreateAsync(new ActivityInput
            {
                EntityType = model.EntityType,
                EntityId = model.EntityId,
                Type = model.Type,
                Subject = model.Subject,
                Notes = model.Notes,
                OccurredAt = model.OccurredAt
            }, _users.GetUserId(User));

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? $"{model.Type} logged."
                : result.Errors.FirstOrDefault()?.Message ?? "The activity could not be logged.";

            return Back(model.ReturnUrl, model.EntityType, model.EntityId);
        }

        [HttpPost("{id:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl)
        {
            var result = await _activities.DeleteAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData["Success"] = "Activity deleted.";

            return Back(returnUrl, result.Value!.EntityType, result.Value.EntityId);
        }

        /// <summary>
        /// Returns to the posted URL when it is local, otherwise rebuilds the detail
        /// route from the record the activity belongs to.
        /// </summary>
        private IActionResult Back(string? returnUrl, CrmEntityType entityType, int entityId)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            var controller = entityType switch
            {
                CrmEntityType.Lead => "CrmLead",
                CrmEntityType.Contact => "CrmContact",
                CrmEntityType.Company => "CrmCompany",
                _ => "CrmDeal"
            };

            return RedirectToAction("Details", controller, new { id = entityId });
        }

        private string? FirstError() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
    }
}
