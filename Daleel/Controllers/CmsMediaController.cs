using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Controllers
{
    [Route("cms/media")]
    [Authorize(Roles = "Admin")]
    public class CmsMediaController : Controller
    {
        private const long MaxUploadSizeBytes = 10 * 1024 * 1024; // 10 MB
        private readonly IMediaLibraryService _media;

        public CmsMediaController(IMediaLibraryService media)
        {
            _media = media;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? folder, string? q)
        {
            var files = await _media.GetFilesAsync(new MediaLibraryQuery
            {
                Folder = folder,
                SearchTerm = q
            });

            var folders = await _media.GetFoldersAsync();

            var model = new MediaLibraryViewModel
            {
                Files = files,
                Folders = folders,
                SelectedFolder = folder,
                SearchTerm = q
            };

            return View(model);
        }

        [HttpPost("upload")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(MediaUploadViewModel model)
        {
            if (model.File == null || model.File.Length == 0)
            {
                TempData["Error"] = "Please select a valid file to upload.";
                return RedirectToAction(nameof(Index), new { folder = model.Folder });
            }

            if (model.File.Length > MaxUploadSizeBytes)
            {
                TempData["Error"] = "File size exceeds the 10 MB limit.";
                return RedirectToAction(nameof(Index), new { folder = model.Folder });
            }

            using var stream = model.File.OpenReadStream();
            var result = await _media.UploadFileAsync(stream, model.File.FileName, model.Folder);

            if (!result.Succeeded)
            {
                TempData["Error"] = result.FirstErrorMessage ?? "Failed to upload file.";
            }
            else
            {
                TempData["Success"] = $"File \"{model.File.FileName}\" uploaded successfully.";
            }

            return RedirectToAction(nameof(Index), new { folder = model.Folder });
        }

        [HttpPost("api/upload")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadApi(IFormFile? file, string? folder = "pages")
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Please select a valid file to upload." });
            }

            if (file.Length > MaxUploadSizeBytes)
            {
                return Json(new { success = false, message = "File size exceeds the 10 MB limit." });
            }

            var safeFolder = string.IsNullOrWhiteSpace(folder) ? "pages" : folder.Trim();
            using var stream = file.OpenReadStream();
            var result = await _media.UploadFileAsync(stream, file.FileName, safeFolder);

            if (!result.Succeeded)
            {
                return Json(new { success = false, message = result.FirstErrorMessage ?? "Failed to upload file." });
            }

            return Json(new { success = true, url = result.Value, fileName = file.FileName });
        }

        [HttpGet("check-usage")]
        public async Task<IActionResult> CheckUsage(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Json(new { inUse = false, count = 0, usages = Array.Empty<MediaUsageDto>() });
            }

            var usages = await _media.GetFileUsagesAsync(path);
            return Json(new
            {
                inUse = usages.Count > 0,
                count = usages.Count,
                usages = usages
            });
        }

        [HttpPost("delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string path, string? folder, bool force = false)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                TempData["Error"] = "File path is required.";
                return RedirectToAction(nameof(Index), new { folder });
            }

            if (!force)
            {
                var usages = await _media.GetFileUsagesAsync(path);
                if (usages.Count > 0)
                {
                    TempData["Warning"] = $"File is in use in {usages.Count} place(s). Force deletion required.";
                    return RedirectToAction(nameof(Index), new { folder });
                }
            }

            var result = await _media.DeleteFileAsync(path);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.FirstErrorMessage ?? "Failed to delete file.";
            }
            else
            {
                TempData["Success"] = "File was deleted successfully.";
            }

            return RedirectToAction(nameof(Index), new { folder });
        }
    }
}
