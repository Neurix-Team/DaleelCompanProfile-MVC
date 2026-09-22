using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Daleel.Controllers
{
    [Route("cms/team")]
    [Authorize(Roles = "Admin")]
    public class TeamMemberController : Controller
    {
        private const long MaxPhotoSizeBytes = 4 * 1024 * 1024; // 4 MB
        private readonly ITeamMemberService _team;
        private readonly IBrandProfileService _brands;
        private readonly IFileStorageService _files;

        public TeamMemberController(
            ITeamMemberService team,
            IBrandProfileService brands,
            IFileStorageService files)
        {
            _team = team;
            _brands = brands;
            _files = files;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] TeamMemberQuery query)
        {
            var paged = await _team.SearchAsync(query);
            var brands = await _brands.GetAllAsync();

            ViewData["Brands"] = brands;
            ViewData["CurrentBrandId"] = query.BrandId;
            ViewData["CurrentQ"] = query.Q;
            ViewData["CurrentIsPublished"] = query.IsPublished;

            return View(paged);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create([FromQuery] int? brandId)
        {
            var brands = await _brands.GetAllAsync();
            var model = new TeamMemberFormViewModel
            {
                BrandId = brandId ?? brands.FirstOrDefault()?.Id ?? 0,
                BrandOptions = new SelectList(brands, "Id", "NameEn")
            };

            return View(model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TeamMemberFormViewModel model)
        {
            if (model.PhotoFile != null && model.PhotoFile.Length > MaxPhotoSizeBytes)
            {
                ModelState.AddModelError(nameof(model.PhotoFile), "Photo size must not exceed 4 MB.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateBrandOptionsAsync(model);
                return View(model);
            }

            string? photoPath = null;
            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                using var stream = model.PhotoFile.OpenReadStream();
                photoPath = await _files.SaveFileAsync(stream, model.PhotoFile.FileName, "team");
            }

            var input = new TeamMemberInput
            {
                BrandId = model.BrandId,
                Slug = model.Slug,
                NameEn = model.NameEn,
                NameAr = model.NameAr,
                TitleEn = model.TitleEn,
                TitleAr = model.TitleAr,
                BioEn = model.BioEn,
                BioAr = model.BioAr,
                Email = model.Email,
                LinkedInUrl = model.LinkedInUrl,
                PhotoPath = photoPath,
                DisplayOrder = model.DisplayOrder,
                IsPublished = model.IsPublished
            };

            var result = await _team.CreateAsync(input);
            if (!result.Succeeded)
            {
                AddErrors(result.Errors);
                await PopulateBrandOptionsAsync(model);
                return View(model);
            }

            TempData["Success"] = $"Team member \"{result.Value!.NameEn}\" was added successfully.";
            return RedirectToAction(nameof(Index), new { brandId = model.BrandId });
        }

        [HttpGet("{id:int}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var member = await _team.GetByIdAsync(id);
            if (member is null)
            {
                return NotFound();
            }

            var brands = await _brands.GetAllAsync();
            var model = new TeamMemberFormViewModel
            {
                Id = member.Id,
                BrandId = member.BrandId,
                Slug = member.Slug,
                NameEn = member.NameEn,
                NameAr = member.NameAr,
                TitleEn = member.TitleEn,
                TitleAr = member.TitleAr,
                BioEn = member.BioEn,
                BioAr = member.BioAr,
                Email = member.Email,
                LinkedInUrl = member.LinkedInUrl,
                ExistingPhotoPath = member.PhotoPath,
                DisplayOrder = member.DisplayOrder,
                IsPublished = member.IsPublished,
                BrandOptions = new SelectList(brands, "Id", "NameEn", member.BrandId)
            };

            return View(model);
        }

        [HttpPost("{id:int}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TeamMemberFormViewModel model)
        {
            if (model.PhotoFile != null && model.PhotoFile.Length > MaxPhotoSizeBytes)
            {
                ModelState.AddModelError(nameof(model.PhotoFile), "Photo size must not exceed 4 MB.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateBrandOptionsAsync(model);
                return View(model);
            }

            string? photoPath = model.ExistingPhotoPath;
            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                using var stream = model.PhotoFile.OpenReadStream();
                var newPath = await _files.SaveFileAsync(stream, model.PhotoFile.FileName, "team");
                if (newPath != null)
                {
                    if (!string.IsNullOrWhiteSpace(photoPath) && !photoPath.StartsWith("/assets/"))
                    {
                        _files.DeleteFile(photoPath);
                    }
                    photoPath = newPath;
                }
            }

            var input = new TeamMemberInput
            {
                BrandId = model.BrandId,
                Slug = model.Slug,
                NameEn = model.NameEn,
                NameAr = model.NameAr,
                TitleEn = model.TitleEn,
                TitleAr = model.TitleAr,
                BioEn = model.BioEn,
                BioAr = model.BioAr,
                Email = model.Email,
                LinkedInUrl = model.LinkedInUrl,
                PhotoPath = photoPath,
                DisplayOrder = model.DisplayOrder,
                IsPublished = model.IsPublished
            };

            var result = await _team.UpdateAsync(id, input);
            if (result.NotFound)
            {
                return NotFound();
            }

            if (!result.Succeeded)
            {
                AddErrors(result.Errors);
                await PopulateBrandOptionsAsync(model);
                return View(model);
            }

            TempData["Success"] = $"Team member \"{result.Value!.NameEn}\" was updated successfully.";
            return RedirectToAction(nameof(Index), new { brandId = model.BrandId });
        }

        [HttpPost("{id:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _team.DeleteAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData["Success"] = "Team member was removed successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id:int}/toggle-publish")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var result = await _team.TogglePublishAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }

            var status = result.Value ? "published" : "set to draft";
            TempData["Success"] = $"Team member is now {status}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateBrandOptionsAsync(TeamMemberFormViewModel model)
        {
            var brands = await _brands.GetAllAsync();
            model.BrandOptions = new SelectList(brands, "Id", "NameEn", model.BrandId);
        }

        private void AddErrors(IReadOnlyList<ServiceError> errors)
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }
        }
    }
}
