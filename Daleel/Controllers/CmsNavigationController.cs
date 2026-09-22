using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Entities;
using Daleel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Controllers
{
    [Route("cms/navigation")]
    [Authorize(Roles = "Admin")]
    public class CmsNavigationController : Controller
    {
        private readonly INavigationService _nav;

        public CmsNavigationController(INavigationService nav)
        {
            _nav = nav;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] string tab = "header")
        {
            var grouped = await _nav.GetGroupedNavigationAsync();
            var model = new NavigationIndexViewModel
            {
                Navigation = grouped,
                ActiveTab = string.IsNullOrWhiteSpace(tab) ? "header" : tab.ToLowerInvariant()
            };

            return View(model);
        }

        [HttpGet("create")]
        public IActionResult Create([FromQuery] NavigationLocation? location, [FromQuery] FooterSection? section)
        {
            var model = new NavigationFormViewModel
            {
                Location = location ?? NavigationLocation.Header,
                Section = section ?? (location == NavigationLocation.Footer ? FooterSection.Platform : null),
                IsActive = true,
                SortOrder = 0
            };

            return View(model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NavigationFormViewModel model)
        {
            if (model.Location == NavigationLocation.Footer && !model.Section.HasValue)
            {
                ModelState.AddModelError(nameof(model.Section), "Footer section / column is required for footer links.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var input = new NavigationLinkInput
            {
                Location = model.Location,
                Section = model.Location == NavigationLocation.Footer ? model.Section : null,
                LabelEn = model.LabelEn,
                LabelAr = model.LabelAr,
                Url = model.Url,
                SortOrder = model.SortOrder,
                IsActive = model.IsActive,
                OpenInNewTab = model.OpenInNewTab
            };

            var result = await _nav.CreateAsync(input);
            if (!result.Succeeded)
            {
                AddErrors(result.Errors);
                return View(model);
            }

            TempData["Success"] = "Navigation link created successfully.";
            var tab = DetermineTab(model.Location, model.Section);
            return RedirectToAction(nameof(Index), new { tab });
        }

        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var link = await _nav.GetByIdAsync(id);
            if (link == null)
            {
                return NotFound();
            }

            var model = new NavigationFormViewModel
            {
                Id = link.Id,
                Location = link.Location,
                Section = link.Section,
                LabelEn = link.LabelEn,
                LabelAr = link.LabelAr,
                Url = link.Url,
                SortOrder = link.SortOrder,
                IsActive = link.IsActive,
                OpenInNewTab = link.OpenInNewTab
            };

            return View(model);
        }

        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NavigationFormViewModel model)
        {
            if (model.Location == NavigationLocation.Footer && !model.Section.HasValue)
            {
                ModelState.AddModelError(nameof(model.Section), "Footer section / column is required for footer links.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var input = new NavigationLinkInput
            {
                Location = model.Location,
                Section = model.Location == NavigationLocation.Footer ? model.Section : null,
                LabelEn = model.LabelEn,
                LabelAr = model.LabelAr,
                Url = model.Url,
                SortOrder = model.SortOrder,
                IsActive = model.IsActive,
                OpenInNewTab = model.OpenInNewTab
            };

            var result = await _nav.UpdateAsync(id, input);
            if (result.NotFound)
            {
                return NotFound();
            }

            if (!result.Succeeded)
            {
                AddErrors(result.Errors);
                return View(model);
            }

            TempData["Success"] = "Navigation link updated successfully.";
            var tab = DetermineTab(model.Location, model.Section);
            return RedirectToAction(nameof(Index), new { tab });
        }

        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, [FromQuery] string tab = "header")
        {
            var result = await _nav.DeleteAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData["Success"] = "Navigation link deleted successfully.";
            return RedirectToAction(nameof(Index), new { tab });
        }

        [HttpPost("toggle-active/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id, [FromQuery] string tab = "header")
        {
            var result = await _nav.ToggleActiveAsync(id);
            if (result.NotFound)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Link not found." });
                }
                return NotFound();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, isActive = result.Value });
            }

            TempData["Success"] = "Status updated successfully.";
            return RedirectToAction(nameof(Index), new { tab });
        }

        [HttpPost("reorder")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder([FromBody] List<int> orderedIds)
        {
            if (orderedIds == null || orderedIds.Count == 0)
            {
                return Json(new { success = false, message = "No items provided." });
            }

            var result = await _nav.ReorderAsync(orderedIds);
            return Json(new { success = result.Succeeded });
        }

        private static string DetermineTab(NavigationLocation location, FooterSection? section)
        {
            if (location == NavigationLocation.Header) return "header";
            return section switch
            {
                FooterSection.Company => "company",
                FooterSection.StayInformed => "stayinformed",
                _ => "platform"
            };
        }

        private void AddErrors(IReadOnlyList<ServiceError> errors)
        {
            foreach (var error in errors)
            {
                if (string.IsNullOrEmpty(error.Field))
                {
                    ModelState.AddModelError(string.Empty, error.Message);
                }
                else
                {
                    ModelState.AddModelError(error.Field, error.Message);
                }
            }
        }
    }
}
