using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Entities;
using Daleel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Controllers
{
    /// <summary>
    /// CRUD for customer organisations. All data access goes through
    /// <see cref="ICompanyService"/> — the controller only maps between the web
    /// layer's view models and the business layer's inputs.
    /// </summary>
    [Route("crm/companies")]
    [Authorize]
    public class CrmCompanyController : Controller
    {
        private readonly ICompanyService _companies;

        public CrmCompanyController(ICompanyService companies) => _companies = companies;

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q, RecordStatus? status, int page = 1)
        {
            var result = await _companies.SearchAsync(new CompanyQuery
            {
                Q = q,
                Status = status,
                Page = page
            });

            ViewData["Q"] = q;
            ViewData["Status"] = status;

            return View(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var company = await _companies.GetWithContactsAsync(id);
            if (company is null)
            {
                return NotFound();
            }

            return View(company);
        }

        [HttpGet("create")]
        public IActionResult Create() => View(new CompanyFormViewModel());

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CompanyFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _companies.CreateAsync(ToInput(model));
            if (!result.Succeeded)
            {
                AddErrors(result.Errors);
                return View(model);
            }

            TempData["Success"] = $"Company \"{result.Value!.Name}\" was created.";
            return RedirectToAction(nameof(Details), new { id = result.Value.Id });
        }

        [HttpGet("{id:int}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var company = await _companies.GetAsync(id);
            if (company is null)
            {
                return NotFound();
            }

            ViewData["CompanyName"] = company.Name;

            return View(new CompanyFormViewModel
            {
                Name = company.Name,
                Industry = company.Industry,
                Website = company.Website,
                Phone = company.Phone,
                Email = company.Email,
                Address = company.Address,
                City = company.City,
                Country = company.Country,
                Description = company.Description,
                Status = company.Status
            });
        }

        [HttpPost("{id:int}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CompanyFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var existing = await _companies.GetAsync(id);
                if (existing is null)
                {
                    return NotFound();
                }

                ViewData["CompanyName"] = existing.Name;
                return View(model);
            }

            var result = await _companies.UpdateAsync(id, ToInput(model));
            if (result.NotFound)
            {
                return NotFound();
            }

            if (!result.Succeeded)
            {
                ViewData["CompanyName"] = model.Name;
                AddErrors(result.Errors);
                return View(model);
            }

            TempData["Success"] = $"Company \"{result.Value!.Name}\" was updated.";
            return RedirectToAction(nameof(Details), new { id = result.Value.Id });
        }

        [HttpPost("{id:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _companies.DeleteAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }

            var deleted = result.Value!;
            TempData["Success"] = deleted.OrphanedContacts > 0
                ? $"Company \"{deleted.Name}\" was deleted. {deleted.OrphanedContacts} contact(s) are no longer linked to a company."
                : $"Company \"{deleted.Name}\" was deleted.";

            return RedirectToAction(nameof(Index));
        }

        private static CompanyInput ToInput(CompanyFormViewModel model) => new()
        {
            Name = model.Name,
            Industry = model.Industry,
            Website = model.Website,
            Phone = model.Phone,
            Email = model.Email,
            Address = model.Address,
            City = model.City,
            Country = model.Country,
            Description = model.Description,
            Status = model.Status
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
