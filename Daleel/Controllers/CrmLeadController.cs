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
    /// Lead management. Leads are archived rather than hard-deleted so their history
    /// survives. All data access goes through the business layer.
    /// </summary>
    [Route("crm/leads")]
    [Authorize]
    public class CrmLeadController : Controller
    {
        private readonly ILeadService _leads;
        private readonly ICompanyService _companies;

        public CrmLeadController(ILeadService leads, ICompanyService companies)
        {
            _leads = leads;
            _companies = companies;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            string? q,
            LeadStatus? status,
            LeadSource? source,
            string? assignedToId,
            bool showArchived = false,
            int page = 1)
        {
            var result = await _leads.SearchAsync(new LeadQuery
            {
                Q = q,
                Status = status,
                Source = source,
                AssignedToId = assignedToId,
                ShowArchived = showArchived,
                Page = page
            });

            ViewData["Q"] = q;
            ViewData["Status"] = status;
            ViewData["Source"] = source;
            ViewData["AssignedToId"] = assignedToId;
            ViewData["ShowArchived"] = showArchived;
            ViewData["AssignedToOptions"] = await BuildUserOptionsAsync();

            return View(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var lead = await _leads.GetWithRelationsAsync(id);
            if (lead is null)
            {
                return NotFound();
            }

            return View(lead);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            var model = new LeadFormViewModel
            {
                AssignedToOptions = await BuildUserOptionsAsync()
            };
            return View(model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeadFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _leads.CreateAsync(ToInput(model));
                if (result.Succeeded)
                {
                    TempData["Success"] = $"Lead \"{result.Value!.FullName}\" was created.";
                    return RedirectToAction(nameof(Details), new { id = result.Value.Id });
                }

                AddErrors(result.Errors);
            }

            model.AssignedToOptions = await BuildUserOptionsAsync();
            return View(model);
        }

        [HttpGet("{id:int}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var lead = await _leads.GetAsync(id);
            if (lead is null)
            {
                return NotFound();
            }

            ViewData["LeadName"] = lead.FullName;

            return View(new LeadFormViewModel
            {
                FirstName = lead.FirstName,
                LastName = lead.LastName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                JobTitle = lead.JobTitle,
                Status = lead.Status,
                Source = lead.Source,
                AssignedToId = lead.AssignedToId,
                Notes = lead.Notes,
                AssignedToOptions = await BuildUserOptionsAsync()
            });
        }

        [HttpPost("{id:int}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LeadFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _leads.UpdateAsync(id, ToInput(model));
                if (result.NotFound)
                {
                    return NotFound();
                }

                if (result.Succeeded)
                {
                    TempData["Success"] = $"Lead \"{result.Value!.FullName}\" was updated.";
                    return RedirectToAction(nameof(Details), new { id = result.Value.Id });
                }

                AddErrors(result.Errors);
            }

            var existing = await _leads.GetAsync(id);
            if (existing is null)
            {
                return NotFound();
            }

            ViewData["LeadName"] = existing.FullName;
            model.AssignedToOptions = await BuildUserOptionsAsync();
            return View(model);
        }

        [HttpGet("{id:int}/convert")]
        public async Task<IActionResult> Convert(int id)
        {
            var context = await _leads.GetConversionContextAsync(id);
            if (context.NotFound)
            {
                return NotFound();
            }

            if (!context.Succeeded)
            {
                TempData["Error"] = context.Errors[0].Message;
                return RedirectToAction(nameof(Details), new { id });
            }

            var data = context.Value!;

            var model = new LeadConversionViewModel
            {
                FirstName = data.FirstName,
                LastName = data.LastName,
                Email = data.Email,
                Phone = data.Phone,
                JobTitle = data.JobTitle,
                LeadName = data.LeadName,
                DuplicateContact = data.DuplicateContact,
                ExistingContactId = data.DuplicateContact?.Id,
                // Pre-selected rather than forced: a matching email is a strong hint,
                // but two people can share a mailbox (info@, sales@).
                LinkExistingContact = data.DuplicateContact is not null,
                // An existing company by the same name wins, so conversion does not
                // create a duplicate organisation.
                ExistingCompanyId = data.MatchedCompanyId
                    ?? (data.UnmatchedCompanyName is null
                        ? null
                        : LeadConversionViewModel.CreateNewCompanyId),
                NewCompanyName = data.UnmatchedCompanyName,
                CompanyOptions = await BuildCompanyOptionsAsync()
            };

            return View(model);
        }

        [HttpPost("{id:int}/convert")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(int id, LeadConversionViewModel model)
        {
            if (model.LinkExistingContact)
            {
                // The contact and company fields are ignored when linking, so their
                // validation is dropped too — otherwise a stale value in a hidden field
                // could block a conversion that never uses it.
                foreach (var key in new[]
                {
                    nameof(model.FirstName),
                    nameof(model.LastName),
                    nameof(model.Email),
                    nameof(model.Phone),
                    nameof(model.JobTitle),
                    nameof(model.ExistingCompanyId),
                    nameof(model.NewCompanyName)
                })
                {
                    ModelState.Remove(key);
                }
            }

            if (ModelState.IsValid)
            {
                var result = await _leads.ConvertAsync(id, ToInput(model));
                if (result.NotFound)
                {
                    return NotFound();
                }

                if (result.Succeeded)
                {
                    var converted = result.Value!;
                    TempData["Success"] = converted.LinkedExisting
                        ? $"Lead \"{converted.LeadName}\" was linked to existing contact \"{converted.ContactName}\"."
                        : $"Lead \"{converted.LeadName}\" was converted to contact \"{converted.ContactName}\".";

                    return RedirectToAction("Details", "CrmContact", new { id = converted.ContactId });
                }

                // A form-level error means the lead itself is no longer convertible
                // (converted or archived in another tab) — send them back to the lead.
                if (result.Errors.Any(e => string.IsNullOrEmpty(e.Field)))
                {
                    TempData["Error"] = result.Errors.First(e => string.IsNullOrEmpty(e.Field)).Message;
                    return RedirectToAction(nameof(Details), new { id });
                }

                AddErrors(result.Errors);
            }

            var context = await _leads.GetConversionContextAsync(id);
            if (context.NotFound)
            {
                return NotFound();
            }

            model.LeadName = context.Value?.LeadName ?? string.Empty;
            model.DuplicateContact = context.Value?.DuplicateContact;
            model.CompanyOptions = await BuildCompanyOptionsAsync();
            return View(model);
        }

        [HttpPost("{id:int}/archive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id) => await SetArchivedAsync(id, true);

        [HttpPost("{id:int}/unarchive")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unarchive(int id) => await SetArchivedAsync(id, false);

        private async Task<IActionResult> SetArchivedAsync(int id, bool archived)
        {
            var result = await _leads.SetArchivedAsync(id, archived);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData["Success"] = archived
                ? $"Lead \"{result.Value!.FullName}\" was archived."
                : $"Lead \"{result.Value!.FullName}\" was restored.";

            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<IEnumerable<SelectListItem>> BuildUserOptionsAsync() =>
            (await _leads.GetAssignableUsersAsync())
                .Select(o => new SelectListItem { Value = o.Value, Text = o.Text })
                .ToList();

        private async Task<IEnumerable<SelectListItem>> BuildCompanyOptionsAsync() =>
            (await _companies.GetOptionsAsync())
                .Select(o => new SelectListItem { Value = o.Value, Text = o.Text })
                .ToList();

        private static LeadInput ToInput(LeadFormViewModel model) => new()
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Phone = model.Phone,
            CompanyName = model.CompanyName,
            JobTitle = model.JobTitle,
            Status = model.Status,
            Source = model.Source,
            AssignedToId = model.AssignedToId,
            Notes = model.Notes
        };

        private static LeadConversionInput ToInput(LeadConversionViewModel model) => new()
        {
            LinkExistingContact = model.LinkExistingContact,
            ExistingContactId = model.ExistingContactId,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            Phone = model.Phone,
            JobTitle = model.JobTitle,
            // The view uses a sentinel option value; the business layer gets an explicit flag.
            CreateNewCompany = model.ExistingCompanyId == LeadConversionViewModel.CreateNewCompanyId,
            ExistingCompanyId = model.ExistingCompanyId == LeadConversionViewModel.CreateNewCompanyId
                ? null
                : model.ExistingCompanyId,
            NewCompanyName = model.NewCompanyName
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
