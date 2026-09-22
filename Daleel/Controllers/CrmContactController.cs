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
    /// CRUD for people. A contact may optionally belong to a company. All data access
    /// goes through the business layer.
    /// </summary>
    [Route("crm/contacts")]
    [Authorize]
    public class CrmContactController : Controller
    {
        private readonly IContactService _contacts;
        private readonly ICompanyService _companies;

        public CrmContactController(IContactService contacts, ICompanyService companies)
        {
            _contacts = contacts;
            _companies = companies;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q, RecordStatus? status, int? companyId, int page = 1)
        {
            var result = await _contacts.SearchAsync(new ContactQuery
            {
                Q = q,
                Status = status,
                CompanyId = companyId,
                Page = page
            });

            ViewData["Q"] = q;
            ViewData["Status"] = status;

            return View(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var contact = await _contacts.GetWithCompanyAsync(id);
            if (contact is null)
            {
                return NotFound();
            }

            return View(contact);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create(int? companyId)
        {
            var model = new ContactFormViewModel
            {
                CompanyId = companyId,
                CompanyOptions = await BuildCompanyOptionsAsync()
            };
            return View(model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContactFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _contacts.CreateAsync(ToInput(model));
                if (result.Succeeded)
                {
                    TempData["Success"] = $"Contact \"{result.Value!.FullName}\" was created.";
                    return RedirectToAction(nameof(Details), new { id = result.Value.Id });
                }

                AddErrors(result.Errors);
            }

            model.CompanyOptions = await BuildCompanyOptionsAsync();
            return View(model);
        }

        [HttpGet("{id:int}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var contact = await _contacts.GetAsync(id);
            if (contact is null)
            {
                return NotFound();
            }

            ViewData["ContactName"] = contact.FullName;

            return View(new ContactFormViewModel
            {
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                JobTitle = contact.JobTitle,
                Email = contact.Email,
                Phone = contact.Phone,
                CompanyId = contact.CompanyId,
                Status = contact.Status,
                Notes = contact.Notes,
                CompanyOptions = await BuildCompanyOptionsAsync()
            });
        }

        [HttpPost("{id:int}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ContactFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _contacts.UpdateAsync(id, ToInput(model));
                if (result.NotFound)
                {
                    return NotFound();
                }

                if (result.Succeeded)
                {
                    TempData["Success"] = $"Contact \"{result.Value!.FullName}\" was updated.";
                    return RedirectToAction(nameof(Details), new { id = result.Value.Id });
                }

                AddErrors(result.Errors);
            }

            var existing = await _contacts.GetAsync(id);
            if (existing is null)
            {
                return NotFound();
            }

            ViewData["ContactName"] = existing.FullName;
            model.CompanyOptions = await BuildCompanyOptionsAsync();
            return View(model);
        }

        [HttpPost("{id:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _contacts.DeleteAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData["Success"] = $"Contact \"{result.Value}\" was deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<IEnumerable<SelectListItem>> BuildCompanyOptionsAsync() =>
            (await _companies.GetOptionsAsync())
                .Select(o => new SelectListItem { Value = o.Value, Text = o.Text })
                .ToList();

        private static ContactInput ToInput(ContactFormViewModel model) => new()
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            JobTitle = model.JobTitle,
            Email = model.Email,
            Phone = model.Phone,
            CompanyId = model.CompanyId,
            Status = model.Status,
            Notes = model.Notes
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
