using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Controllers
{
    [Route("cms/brands")]
    [Authorize(Roles = "Admin")]
    public class CmsBrandController : Controller
    {
        private const long MaxLogoSizeBytes = 3 * 1024 * 1024; // 3 MB
        private const long MaxFaviconSizeBytes = 1 * 1024 * 1024; // 1 MB
        private readonly IBrandProfileService _brands;
        private readonly IFileStorageService _files;

        public CmsBrandController(IBrandProfileService brands, IFileStorageService files)
        {
            _brands = brands;
            _files = files;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var list = await _brands.GetAllAsync();
            return View(list);
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View(new BrandProfileFormViewModel
            {
                PrimaryColor = "#00B2EC",
                SecondaryColor = "#10B981",
                AccentColor = "#F9A01B"
            });
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BrandProfileFormViewModel model)
        {
            if (model.LogoFile != null && model.LogoFile.Length > MaxLogoSizeBytes)
            {
                ModelState.AddModelError(nameof(model.LogoFile), "Logo size must not exceed 3 MB.");
            }

            if (model.LogoDarkFile != null && model.LogoDarkFile.Length > MaxLogoSizeBytes)
            {
                ModelState.AddModelError(nameof(model.LogoDarkFile), "Dark logo size must not exceed 3 MB.");
            }

            if (model.FaviconFile != null && model.FaviconFile.Length > MaxFaviconSizeBytes)
            {
                ModelState.AddModelError(nameof(model.FaviconFile), "Favicon size must not exceed 1 MB.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? logoPath = null;
            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                using var stream = model.LogoFile.OpenReadStream();
                logoPath = await _files.SaveFileAsync(stream, model.LogoFile.FileName, "brands");
            }

            string? logoDarkPath = null;
            if (model.LogoDarkFile != null && model.LogoDarkFile.Length > 0)
            {
                using var stream = model.LogoDarkFile.OpenReadStream();
                logoDarkPath = await _files.SaveFileAsync(stream, model.LogoDarkFile.FileName, "brands");
            }

            string? faviconPath = null;
            if (model.FaviconFile != null && model.FaviconFile.Length > 0)
            {
                using var stream = model.FaviconFile.OpenReadStream();
                faviconPath = await _files.SaveFileAsync(stream, model.FaviconFile.FileName, "brands");
            }

            var input = new BrandProfileInput
            {
                Slug = model.Slug ?? string.Empty,
                NameEn = model.NameEn,
                NameAr = model.NameAr,
                TaglineEn = model.TaglineEn,
                TaglineAr = model.TaglineAr,
                DescriptionEn = model.DescriptionEn,
                DescriptionAr = model.DescriptionAr,
                LogoPath = logoPath,
                LogoDarkPath = logoDarkPath,
                FaviconPath = faviconPath,
                PrimaryColor = model.PrimaryColor,
                SecondaryColor = model.SecondaryColor,
                AccentColor = model.AccentColor,
                MetaTitleEn = model.MetaTitleEn,
                MetaTitleAr = model.MetaTitleAr,
                MetaDescriptionEn = model.MetaDescriptionEn,
                MetaDescriptionAr = model.MetaDescriptionAr,
                GoogleAnalyticsId = model.GoogleAnalyticsId,
                LinkedInUrl = model.LinkedInUrl,
                TwitterUrl = model.TwitterUrl,
                FacebookUrl = model.FacebookUrl,
                InstagramUrl = model.InstagramUrl,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                Website = model.Website,
                IsPublished = model.IsPublished
            };

            var result = await _brands.CreateAsync(input);
            if (!result.Succeeded)
            {
                AddErrors(result.Errors);
                return View(model);
            }

            TempData["Success"] = $"Brand profile \"{result.Value!.NameEn}\" was created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _brands.GetByIdAsync(id);
            if (brand is null)
            {
                return NotFound();
            }

            var model = new BrandProfileFormViewModel
            {
                Id = brand.Id,
                Slug = brand.Slug,
                NameEn = brand.NameEn,
                NameAr = brand.NameAr,
                TaglineEn = brand.TaglineEn,
                TaglineAr = brand.TaglineAr,
                DescriptionEn = brand.DescriptionEn,
                DescriptionAr = brand.DescriptionAr,
                ExistingLogoPath = brand.LogoPath,
                ExistingLogoDarkPath = brand.LogoDarkPath,
                ExistingFaviconPath = brand.FaviconPath,
                PrimaryColor = brand.PrimaryColor,
                SecondaryColor = brand.SecondaryColor,
                AccentColor = brand.AccentColor,
                MetaTitleEn = brand.MetaTitleEn,
                MetaTitleAr = brand.MetaTitleAr,
                MetaDescriptionEn = brand.MetaDescriptionEn,
                MetaDescriptionAr = brand.MetaDescriptionAr,
                GoogleAnalyticsId = brand.GoogleAnalyticsId,
                LinkedInUrl = brand.LinkedInUrl,
                TwitterUrl = brand.TwitterUrl,
                FacebookUrl = brand.FacebookUrl,
                InstagramUrl = brand.InstagramUrl,
                Email = brand.Email,
                Phone = brand.Phone,
                Address = brand.Address,
                Website = brand.Website,
                IsPublished = brand.IsPublished
            };

            return View(model);
        }

        [HttpPost("{id:int}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BrandProfileFormViewModel model)
        {
            if (model.LogoFile != null && model.LogoFile.Length > MaxLogoSizeBytes)
            {
                ModelState.AddModelError(nameof(model.LogoFile), "Logo size must not exceed 3 MB.");
            }

            if (model.LogoDarkFile != null && model.LogoDarkFile.Length > MaxLogoSizeBytes)
            {
                ModelState.AddModelError(nameof(model.LogoDarkFile), "Dark logo size must not exceed 3 MB.");
            }

            if (model.FaviconFile != null && model.FaviconFile.Length > MaxFaviconSizeBytes)
            {
                ModelState.AddModelError(nameof(model.FaviconFile), "Favicon size must not exceed 1 MB.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? logoPath = model.ExistingLogoPath;
            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                using var stream = model.LogoFile.OpenReadStream();
                var newPath = await _files.SaveFileAsync(stream, model.LogoFile.FileName, "brands");
                if (newPath != null)
                {
                    if (!string.IsNullOrWhiteSpace(logoPath) && !logoPath.StartsWith("/assets/") && !logoPath.StartsWith("/favicon"))
                    {
                        _files.DeleteFile(logoPath);
                    }
                    logoPath = newPath;
                }
            }

            string? logoDarkPath = model.ExistingLogoDarkPath;
            if (model.LogoDarkFile != null && model.LogoDarkFile.Length > 0)
            {
                using var stream = model.LogoDarkFile.OpenReadStream();
                var newDarkPath = await _files.SaveFileAsync(stream, model.LogoDarkFile.FileName, "brands");
                if (newDarkPath != null)
                {
                    if (!string.IsNullOrWhiteSpace(logoDarkPath) && !logoDarkPath.StartsWith("/assets/") && !logoDarkPath.StartsWith("/favicon"))
                    {
                        _files.DeleteFile(logoDarkPath);
                    }
                    logoDarkPath = newDarkPath;
                }
            }

            string? faviconPath = model.ExistingFaviconPath;
            if (model.FaviconFile != null && model.FaviconFile.Length > 0)
            {
                using var stream = model.FaviconFile.OpenReadStream();
                var newFavPath = await _files.SaveFileAsync(stream, model.FaviconFile.FileName, "brands");
                if (newFavPath != null)
                {
                    if (!string.IsNullOrWhiteSpace(faviconPath) && !faviconPath.StartsWith("/assets/") && !faviconPath.StartsWith("/favicon"))
                    {
                        _files.DeleteFile(faviconPath);
                    }
                    faviconPath = newFavPath;
                }
            }

            var input = new BrandProfileInput
            {
                Slug = model.Slug ?? string.Empty,
                NameEn = model.NameEn,
                NameAr = model.NameAr,
                TaglineEn = model.TaglineEn,
                TaglineAr = model.TaglineAr,
                DescriptionEn = model.DescriptionEn,
                DescriptionAr = model.DescriptionAr,
                LogoPath = logoPath,
                LogoDarkPath = logoDarkPath,
                FaviconPath = faviconPath,
                PrimaryColor = model.PrimaryColor,
                SecondaryColor = model.SecondaryColor,
                AccentColor = model.AccentColor,
                MetaTitleEn = model.MetaTitleEn,
                MetaTitleAr = model.MetaTitleAr,
                MetaDescriptionEn = model.MetaDescriptionEn,
                MetaDescriptionAr = model.MetaDescriptionAr,
                GoogleAnalyticsId = model.GoogleAnalyticsId,
                LinkedInUrl = model.LinkedInUrl,
                TwitterUrl = model.TwitterUrl,
                FacebookUrl = model.FacebookUrl,
                InstagramUrl = model.InstagramUrl,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                Website = model.Website,
                IsPublished = model.IsPublished
            };

            var result = await _brands.UpdateAsync(id, input);
            if (result.NotFound)
            {
                return NotFound();
            }

            if (!result.Succeeded)
            {
                AddErrors(result.Errors);
                return View(model);
            }

            TempData["Success"] = $"Brand profile \"{result.Value!.NameEn}\" was updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _brands.DeleteAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData["Success"] = "Brand profile was removed successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id:int}/toggle-publish")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var result = await _brands.TogglePublishAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }

            var status = result.Value ? "published" : "set to draft";
            TempData["Success"] = $"Brand profile is now {status}.";
            return RedirectToAction(nameof(Index));
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
