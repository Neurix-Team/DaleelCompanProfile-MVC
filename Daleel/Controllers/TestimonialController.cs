using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Daleel.Controllers
{
    [Route("cms/testimonials")]
    [Authorize(Roles = "Admin")]
    public class TestimonialController : Controller
    {
        private const long MaxPhotoSizeBytes = 4 * 1024 * 1024; // 4 MB
        private readonly ITestimonialService _testimonials;
        private readonly IBrandProfileService _brands;
        private readonly IFileStorageService _files;

        public TestimonialController(
            ITestimonialService testimonials,
            IBrandProfileService brands,
            IFileStorageService files)
        {
            _testimonials = testimonials;
            _brands = brands;
            _files = files;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] TestimonialQuery query)
        {
            var paged = await _testimonials.SearchAsync(query);
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
            var model = new TestimonialFormViewModel
            {
                BrandId = brandId ?? brands.FirstOrDefault()?.Id ?? 0,
                BrandOptions = new SelectList(brands, "Id", "NameEn")
            };

            return View(model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TestimonialFormViewModel model)
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
                photoPath = await _files.SaveFileAsync(stream, model.PhotoFile.FileName, "testimonials");
            }

            var input = new TestimonialInput
            {
                BrandId = model.BrandId,
                CustomerNameEn = model.CustomerNameEn,
                CustomerNameAr = model.CustomerNameAr,
                CompanyNameEn = model.CompanyNameEn,
                CompanyNameAr = model.CompanyNameAr,
                RoleTitleEn = model.RoleTitleEn,
                RoleTitleAr = model.RoleTitleAr,
                ContentEn = model.ContentEn,
                ContentAr = model.ContentAr,
                Rating = model.Rating,
                PhotoPath = photoPath,
                DisplayOrder = model.DisplayOrder,
                IsPublished = model.IsPublished
            };

            var result = await _testimonials.CreateAsync(input);
            if (!result.Succeeded)
            {
                AddErrors(result.Errors);
                await PopulateBrandOptionsAsync(model);
                return View(model);
            }

            TempData["Success"] = $"Testimonial by \"{result.Value!.CustomerNameEn}\" was created successfully.";
            return RedirectToAction(nameof(Index), new { brandId = model.BrandId });
        }

        [HttpGet("{id:int}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _testimonials.GetByIdAsync(id);
            if (item is null)
            {
                return NotFound();
            }

            var brands = await _brands.GetAllAsync();
            var model = new TestimonialFormViewModel
            {
                Id = item.Id,
                BrandId = item.BrandId,
                CustomerNameEn = item.CustomerNameEn,
                CustomerNameAr = item.CustomerNameAr,
                CompanyNameEn = item.CompanyNameEn,
                CompanyNameAr = item.CompanyNameAr,
                RoleTitleEn = item.RoleTitleEn,
                RoleTitleAr = item.RoleTitleAr,
                ContentEn = item.ContentEn,
                ContentAr = item.ContentAr,
                Rating = item.Rating,
                ExistingPhotoPath = item.PhotoPath,
                DisplayOrder = item.DisplayOrder,
                IsPublished = item.IsPublished,
                BrandOptions = new SelectList(brands, "Id", "NameEn", item.BrandId)
            };

            return View(model);
        }

        [HttpPost("{id:int}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TestimonialFormViewModel model)
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
                var newPath = await _files.SaveFileAsync(stream, model.PhotoFile.FileName, "testimonials");
                if (newPath != null)
                {
                    if (!string.IsNullOrWhiteSpace(photoPath) && !photoPath.StartsWith("/assets/"))
                    {
                        _files.DeleteFile(photoPath);
                    }
                    photoPath = newPath;
                }
            }

            var input = new TestimonialInput
            {
                BrandId = model.BrandId,
                CustomerNameEn = model.CustomerNameEn,
                CustomerNameAr = model.CustomerNameAr,
                CompanyNameEn = model.CompanyNameEn,
                CompanyNameAr = model.CompanyNameAr,
                RoleTitleEn = model.RoleTitleEn,
                RoleTitleAr = model.RoleTitleAr,
                ContentEn = model.ContentEn,
                ContentAr = model.ContentAr,
                Rating = model.Rating,
                PhotoPath = photoPath,
                DisplayOrder = model.DisplayOrder,
                IsPublished = model.IsPublished
            };

            var result = await _testimonials.UpdateAsync(id, input);
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

            TempData["Success"] = $"Testimonial by \"{result.Value!.CustomerNameEn}\" was updated successfully.";
            return RedirectToAction(nameof(Index), new { brandId = model.BrandId });
        }

        [HttpPost("{id:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _testimonials.DeleteAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData["Success"] = "Testimonial was removed successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id:int}/toggle-publish")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var result = await _testimonials.TogglePublishAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }

            var status = result.Value ? "published" : "set to draft";
            TempData["Success"] = $"Testimonial is now {status}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateBrandOptionsAsync(TestimonialFormViewModel model)
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
