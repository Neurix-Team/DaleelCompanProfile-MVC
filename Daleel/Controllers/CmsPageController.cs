using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Controllers
{
    [Route("cms/pages")]
    [Authorize(Roles = "Admin")]
    public class CmsPageController : Controller
    {
        private readonly IPageSectionService _pageSections;

        public CmsPageController(IPageSectionService pageSections)
        {
            _pageSections = pageSections;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var pages = await _pageSections.GetDistinctPagesAsync();
            return View(pages);
        }

        [HttpGet("{pageKey}")]
        public async Task<IActionResult> Edit(string pageKey)
        {
            if (string.IsNullOrWhiteSpace(pageKey))
            {
                return RedirectToAction(nameof(Index));
            }

            await _pageSections.SyncMissingSectionKeysAsync();

            var sections = await _pageSections.GetSectionsByPageAsync(pageKey);
            var model = new PageSectionsEditViewModel
            {
                PageKey = pageKey,
                Sections = sections.Select(s => new PageSectionInput
                {
                    PageKey = s.PageKey,
                    SectionKey = s.SectionKey,
                    ValueEn = s.ValueEn,
                    ValueAr = s.ValueAr,
                    DataType = s.DataType
                }).ToList()
            };

            return View(model);
        }

        [HttpPost("{pageKey}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string pageKey, PageSectionsEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _pageSections.BulkUpsertAsync(pageKey, model.Sections);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Failed to save changes.";
                return View(model);
            }

            TempData["Success"] = $"Page sections for \"{pageKey}\" were updated successfully.";
            return RedirectToAction(nameof(Edit), new { pageKey });
        }

        [HttpPost("{pageKey}/add-section")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSection(string pageKey, PageSectionInput input)
        {
            if (string.IsNullOrWhiteSpace(input.SectionKey))
            {
                TempData["Error"] = "Section key is required.";
                return RedirectToAction(nameof(Edit), new { pageKey });
            }

            input.PageKey = pageKey;
            await _pageSections.UpsertAsync(input);

            TempData["Success"] = $"Section \"{input.SectionKey}\" added.";
            return RedirectToAction(nameof(Edit), new { pageKey });
        }

        [HttpPost("{pageKey}/delete-section")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSection(string pageKey, string sectionKey)
        {
            if (string.IsNullOrWhiteSpace(sectionKey))
            {
                TempData["Error"] = "Section key is required.";
                return RedirectToAction(nameof(Edit), new { pageKey });
            }

            var result = await _pageSections.DeleteSectionAsync(pageKey, sectionKey);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Failed to delete section.";
            }
            else
            {
                TempData["Success"] = $"Section \"{sectionKey}\" was deleted.";
            }

            return RedirectToAction(nameof(Edit), new { pageKey });
        }
    }
}
