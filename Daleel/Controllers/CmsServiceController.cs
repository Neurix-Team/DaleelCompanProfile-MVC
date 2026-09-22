using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Daleel.Controllers
{
    [Route("cms/services")]
    [Authorize(Roles = "Admin")]
    public class CmsServiceController : Controller
    {
        private const long MaxImageSizeBytes = 4 * 1024 * 1024; // 4 MB
        private readonly ICmsServiceService _services;
        private readonly IBrandProfileService _brands;
        private readonly IFileStorageService _files;

        public CmsServiceController(
            ICmsServiceService services,
            IBrandProfileService brands,
            IFileStorageService files)
        {
            _services = services;
            _brands = brands;
            _files = files;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] CmsServiceQuery query)
        {
            var paged = await _services.SearchAsync(query);
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
            var model = new CmsServiceFormViewModel
            {
                BrandId = brandId ?? brands.FirstOrDefault()?.Id ?? 0,
                BrandOptions = new SelectList(brands, "Id", "NameEn")
            };

            return View(model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CmsServiceFormViewModel model)
        {
            if (model.ImageFile != null && model.ImageFile.Length > MaxImageSizeBytes)
            {
                ModelState.AddModelError(nameof(model.ImageFile), "Image size must not exceed 4 MB.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateBrandOptionsAsync(model);
                return View(model);
            }

            string? imagePath = null;
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                using var stream = model.ImageFile.OpenReadStream();
                imagePath = await _files.SaveFileAsync(stream, model.ImageFile.FileName, "services");
            }

            var input = new CmsServiceInput
            {
                BrandId = model.BrandId,
                Slug = model.Slug,
                NameEn = model.NameEn,
                NameAr = model.NameAr,
                ShortDescEn = model.ShortDescEn,
                ShortDescAr = model.ShortDescAr,
                DescriptionEn = model.DescriptionEn,
                DescriptionAr = model.DescriptionAr,
                IconName = model.IconName,
                ImagePath = imagePath,
                DisplayOrder = model.DisplayOrder,
                IsPublished = model.IsPublished
            };

            var result = await _services.CreateAsync(input);
            if (!result.Succeeded)
            {
                AddErrors(result.Errors);
                await PopulateBrandOptionsAsync(model);
                return View(model);
            }

            TempData["Success"] = $"Service \"{result.Value!.NameEn}\" was created successfully.";
            return RedirectToAction(nameof(Index), new { brandId = model.BrandId });
        }

        [HttpGet("{id:int}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var service = await _services.GetByIdAsync(id);
            if (service is null)
            {
                return NotFound();
            }

            var brands = await _brands.GetAllAsync();
            var model = new CmsServiceFormViewModel
            {
                Id = service.Id,
                BrandId = service.BrandId,
                Slug = service.Slug,
                NameEn = service.NameEn,
                NameAr = service.NameAr,
                ShortDescEn = service.ShortDescEn,
                ShortDescAr = service.ShortDescAr,
                DescriptionEn = service.DescriptionEn,
                DescriptionAr = service.DescriptionAr,
                IconName = service.IconName,
                ExistingImagePath = service.ImagePath,
                DisplayOrder = service.DisplayOrder,
                IsPublished = service.IsPublished,
                BrandOptions = new SelectList(brands, "Id", "NameEn", service.BrandId)
            };

            return View(model);
        }

        [HttpPost("{id:int}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CmsServiceFormViewModel model)
        {
            if (model.ImageFile != null && model.ImageFile.Length > MaxImageSizeBytes)
            {
                ModelState.AddModelError(nameof(model.ImageFile), "Image size must not exceed 4 MB.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateBrandOptionsAsync(model);
                return View(model);
            }

            string? imagePath = model.ExistingImagePath;
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                using var stream = model.ImageFile.OpenReadStream();
                var newPath = await _files.SaveFileAsync(stream, model.ImageFile.FileName, "services");
                if (newPath != null)
                {
                    if (!string.IsNullOrWhiteSpace(imagePath) && !imagePath.StartsWith("/assets/"))
                    {
                        _files.DeleteFile(imagePath);
                    }
                    imagePath = newPath;
                }
            }

            var input = new CmsServiceInput
            {
                BrandId = model.BrandId,
                Slug = model.Slug,
                NameEn = model.NameEn,
                NameAr = model.NameAr,
                ShortDescEn = model.ShortDescEn,
                ShortDescAr = model.ShortDescAr,
                DescriptionEn = model.DescriptionEn,
                DescriptionAr = model.DescriptionAr,
                IconName = model.IconName,
                ImagePath = imagePath,
                DisplayOrder = model.DisplayOrder,
                IsPublished = model.IsPublished
            };

            var result = await _services.UpdateAsync(id, input);
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

            TempData["Success"] = $"Service \"{result.Value!.NameEn}\" was updated successfully.";
            return RedirectToAction(nameof(Index), new { brandId = model.BrandId });
        }

        [HttpPost("{id:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _services.DeleteAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData["Success"] = "Service was removed successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id:int}/toggle-publish")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var result = await _services.TogglePublishAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }

            var status = result.Value ? "published" : "set to draft";
            TempData["Success"] = $"Service is now {status}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateBrandOptionsAsync(CmsServiceFormViewModel model)
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
