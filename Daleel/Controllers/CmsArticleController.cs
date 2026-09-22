using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Daleel.Controllers
{
    [Route("cms/articles")]
    [Authorize(Roles = "Admin")]
    public class CmsArticleController : Controller
    {
        private const long MaxCoverImageSizeBytes = 5 * 1024 * 1024; // 5 MB
        private readonly IArticleService _articles;
        private readonly IFileStorageService _files;

        public CmsArticleController(IArticleService articles, IFileStorageService files)
        {
            _articles = articles;
            _files = files;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q, string? category, bool? isPublished, int page = 1)
        {
            var result = await _articles.SearchAsync(new ArticleQuery
            {
                Q = q,
                Category = category,
                IsPublished = isPublished,
                Page = page,
                PageSize = 10
            });

            ViewData["Q"] = q;
            ViewData["Category"] = category;
            ViewData["IsPublished"] = isPublished;

            return View(result);
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View(new ArticleFormViewModel());
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ArticleFormViewModel model)
        {
            if (model.CoverImage != null && model.CoverImage.Length > MaxCoverImageSizeBytes)
            {
                ModelState.AddModelError(nameof(model.CoverImage), "Cover image size must not exceed 5 MB.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? coverImagePath = null;
            if (model.CoverImage != null && model.CoverImage.Length > 0)
            {
                using var stream = model.CoverImage.OpenReadStream();
                coverImagePath = await _files.SaveFileAsync(stream, model.CoverImage.FileName, "articles");
            }

            var input = new ArticleInput
            {
                TitleEn = model.TitleEn,
                TitleAr = model.TitleAr,
                Slug = model.Slug,
                SummaryEn = model.SummaryEn,
                SummaryAr = model.SummaryAr,
                BodyHtmlEn = model.BodyHtmlEn,
                BodyHtmlAr = model.BodyHtmlAr,
                CoverImagePath = coverImagePath,
                Category = model.Category,
                Tags = model.Tags,
                AuthorName = model.AuthorName,
                ReadingMinutes = model.ReadingMinutes,
                IsPublished = model.IsPublished,
                ScheduledPublishAt = model.ScheduledPublishAt,
                MetaTitleEn = model.MetaTitleEn,
                MetaTitleAr = model.MetaTitleAr,
                MetaDescriptionEn = model.MetaDescriptionEn,
                MetaDescriptionAr = model.MetaDescriptionAr,
                CanonicalUrl = model.CanonicalUrl
            };

            var result = await _articles.CreateAsync(input);
            if (!result.Succeeded)
            {
                AddErrors(result.Errors);
                return View(model);
            }

            TempData["Success"] = $"Article \"{result.Value!.TitleEn}\" was created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var article = await _articles.GetByIdAsync(id);
            if (article is null)
            {
                return NotFound();
            }

            var model = new ArticleFormViewModel
            {
                Id = article.Id,
                TitleEn = article.TitleEn,
                TitleAr = article.TitleAr,
                Slug = article.Slug,
                SummaryEn = article.SummaryEn,
                SummaryAr = article.SummaryAr,
                BodyHtmlEn = article.BodyHtmlEn,
                BodyHtmlAr = article.BodyHtmlAr,
                ExistingCoverImagePath = article.CoverImagePath,
                Category = article.Category,
                Tags = article.Tags,
                AuthorName = article.AuthorName,
                ReadingMinutes = article.ReadingMinutes,
                IsPublished = article.IsPublished,
                ScheduledPublishAt = article.ScheduledPublishAt,
                MetaTitleEn = article.MetaTitleEn,
                MetaTitleAr = article.MetaTitleAr,
                MetaDescriptionEn = article.MetaDescriptionEn,
                MetaDescriptionAr = article.MetaDescriptionAr,
                CanonicalUrl = article.CanonicalUrl
            };

            return View(model);
        }

        [HttpPost("{id:int}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ArticleFormViewModel model)
        {
            if (model.CoverImage != null && model.CoverImage.Length > MaxCoverImageSizeBytes)
            {
                ModelState.AddModelError(nameof(model.CoverImage), "Cover image size must not exceed 5 MB.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? coverImagePath = model.ExistingCoverImagePath;
            if (model.CoverImage != null && model.CoverImage.Length > 0)
            {
                using var stream = model.CoverImage.OpenReadStream();
                var newPath = await _files.SaveFileAsync(stream, model.CoverImage.FileName, "articles");
                if (newPath != null)
                {
                    if (!string.IsNullOrWhiteSpace(coverImagePath))
                    {
                        _files.DeleteFile(coverImagePath);
                    }
                    coverImagePath = newPath;
                }
            }

            var input = new ArticleInput
            {
                TitleEn = model.TitleEn,
                TitleAr = model.TitleAr,
                Slug = model.Slug,
                SummaryEn = model.SummaryEn,
                SummaryAr = model.SummaryAr,
                BodyHtmlEn = model.BodyHtmlEn,
                BodyHtmlAr = model.BodyHtmlAr,
                CoverImagePath = coverImagePath,
                Category = model.Category,
                Tags = model.Tags,
                AuthorName = model.AuthorName,
                ReadingMinutes = model.ReadingMinutes,
                IsPublished = model.IsPublished,
                ScheduledPublishAt = model.ScheduledPublishAt,
                MetaTitleEn = model.MetaTitleEn,
                MetaTitleAr = model.MetaTitleAr,
                MetaDescriptionEn = model.MetaDescriptionEn,
                MetaDescriptionAr = model.MetaDescriptionAr,
                CanonicalUrl = model.CanonicalUrl
            };

            var result = await _articles.UpdateAsync(id, input);
            if (result.NotFound)
            {
                return NotFound();
            }

            if (!result.Succeeded)
            {
                AddErrors(result.Errors);
                return View(model);
            }

            TempData["Success"] = $"Article \"{result.Value!.TitleEn}\" was updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id:int}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var article = await _articles.GetByIdAsync(id);
            if (article != null && !string.IsNullOrWhiteSpace(article.CoverImagePath))
            {
                _files.DeleteFile(article.CoverImagePath);
            }

            var result = await _articles.DeleteAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }

            TempData["Success"] = "Article was deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id:int}/toggle-publish")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var result = await _articles.TogglePublishAsync(id);
            if (result.NotFound)
            {
                return NotFound();
            }

            var statusText = result.Value ? "published" : "moved to drafts";
            TempData["Success"] = $"Article was {statusText}.";
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
