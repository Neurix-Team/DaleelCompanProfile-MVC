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
    /// Deals and the sales pipeline. All data access goes through <see cref="IDealService"/>.
    /// </summary>
    [Route("crm/deals")]
    [Authorize]
    public class CrmDealController : Controller
    {
        private readonly IDealService _deals;
        private readonly ICompanyService _companies;
        private readonly ILeadService _leads;

        public CrmDealController(IDealService deals, ICompanyService companies, ILeadService leads)
        {
            _deals = deals;
            _companies = companies;
            _leads = leads;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            string? q,
            DealStage? stage,
            string? assignedToId,
            bool openOnly = false,
            DealSort sort = DealSort.Newest,
            int page = 1)
        {
            var result = await _deals.SearchAsync(new DealQuery
            {
                Q = q,
                Stage = stage,
                AssignedToId = assignedToId,
                OpenOnly = openOnly,
                Sort = sort,
                Page = page
            });

            ViewData["Q"] = q;
            ViewData["Stage"] = stage;
            ViewData["AssignedToId"] = assignedToId;
            ViewData["OpenOnly"] = openOnly;
            ViewData["Sort"] = sort;
            ViewData["AssignedToOptions"] = await BuildUserOptionsAsync();

            return View(result);
        }

        [HttpGet("pipeline")]
        public async Task<IActionResult> Pipeline(string? assignedToId)
        {
            var board = await _deals.GetPipelineAsync(assignedToId);

            ViewData["AssignedToId"] = assignedToId;
            ViewData["AssignedToOptions"] = await BuildUserOptionsAsync();

            return View(board);
        }

        /// <summary>
        /// Stage change from the pipeline board. Returns JSON for the drag-and-drop
        /// path and redirects for the no-JavaScript dropdown fallback.
        /// </summary>
        [HttpPost("{id:int}/move")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Move(int id, DealStage stage, string? returnUrl)
        {
            var result = await _deals.MoveToStageAsync(id, stage);

            if (IsAjax())
            {
                if (result.NotFound)
                {
                    return NotFound(new { message = "That deal no longer exists." });
                }

                return Json(new { ok = true, stage = stage.ToString() });
            }

            if (result.NotFound)
            {
                return NotFound();
            }

            TempData["Success"] = $"\"{result.Value!.Name}\" moved to {stage}.";

            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction(nameof(Pipeline));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var deal = await _deals.GetWithRelationsAsync(id);
            if (deal is null)
            {
                return NotFound();
            }

            return View(deal);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create(int? companyId)
        {
            var model = new DealFormViewModel
            {
                CompanyId = companyId,
                ExpectedCloseDate = DateTime.UtcNow.Date.AddDays(30)
            };

            await PopulateOptionsAsync(model);
            return View(model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DealFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _deals.CreateAsync(ToInput(model));
                if (result.Succeeded)
                {
                    TempData["Success"] = $"Deal \"{result.Value!.Name}\" was created.";
                    return RedirectToAction(nameof(Details), new { id = result.Value.Id });
                }

                AddErrors(result.Errors);
            }

            await PopulateOptionsAsync(model);
            return View(model);
        }

        [HttpGet("{id:int}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var deal = await _deals.GetAsync(id);
            if (deal is null)
            {
                return NotFound();
            }

            ViewData["DealName"] = deal.Name;

            var model = new DealFormViewModel
            {
                Name = deal.Name,
                CompanyId = deal.CompanyId,
                PrimaryContactId = deal.PrimaryContactId,
                Value = deal.Value,
                Currency = deal.Currency,
                Stage = deal.Stage,
                ExpectedCloseDate = deal.ExpectedCloseDate,
                AssignedToId = deal.AssignedToId,
                Description = deal.Description
            };

            await PopulateOptionsAsync(model);
            return View(model);
        }

        [HttpPost("{id:int}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DealFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _deals.UpdateAsync(id, ToInput(model));
                if (result.NotFound)
                {
                    return NotFound();
                }

                if (result.Succeeded)
                {
                    TempData["Success"] = $"Deal \"{result.Value!.Name}\" was updated.";
                    return RedirectToAction(nameof(Details), new { id = result.Value.Id });
                }

                AddErrors(result.Errors);
            }

            var existing = await _deals.GetAsync(id);
            if (existing is null)
            {
                return NotFound();
            }

            ViewData["DealName"] = existing.Name;
            await PopulateOptionsAsync(model);
            return View(model);
        }

        [HttpPost("{id:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _deals.DeleteAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData["Success"] = $"Deal \"{result.Value}\" was deleted.";
            return RedirectToAction(nameof(Index));
        }

        private bool IsAjax() =>
            string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

        private async Task PopulateOptionsAsync(DealFormViewModel model)
        {
            model.CompanyOptions = (await _companies.GetOptionsAsync())
                .Select(o => new SelectListItem { Value = o.Value, Text = o.Text })
                .ToList();

            model.ContactOptions = (await _deals.GetContactOptionsAsync())
                .Select(o => new SelectListItem { Value = o.Value, Text = o.Text })
                .ToList();

            model.AssignedToOptions = await BuildUserOptionsAsync();

            model.CurrencyOptions = _deals.GetCurrencies()
                .Select(c => new SelectListItem { Value = c, Text = c })
                .ToList();
        }

        private async Task<IEnumerable<SelectListItem>> BuildUserOptionsAsync() =>
            (await _leads.GetAssignableUsersAsync())
                .Select(o => new SelectListItem { Value = o.Value, Text = o.Text })
                .ToList();

        private static DealInput ToInput(DealFormViewModel model) => new()
        {
            Name = model.Name,
            CompanyId = model.CompanyId,
            PrimaryContactId = model.PrimaryContactId,
            Value = model.Value,
            Currency = model.Currency,
            Stage = model.Stage,
            ExpectedCloseDate = model.ExpectedCloseDate,
            AssignedToId = model.AssignedToId,
            Description = model.Description
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
