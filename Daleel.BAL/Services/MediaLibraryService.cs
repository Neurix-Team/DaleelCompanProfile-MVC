using Daleel.BAL.Models;
using Daleel.BAL.Services.Interfaces;
using Daleel.DAL.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Daleel.BAL.Services
{
    public class MediaLibraryService : IMediaLibraryService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IFileStorageService _storage;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<MediaLibraryService> _logger;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };

        public MediaLibraryService(
            IWebHostEnvironment env,
            IFileStorageService storage,
            ApplicationDbContext db,
            ILogger<MediaLibraryService> logger)
        {
            _env = env;
            _storage = storage;
            _db = db;
            _logger = logger;
        }

        public Task<IReadOnlyList<MediaFileDto>> GetFilesAsync(MediaLibraryQuery query)
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsRoot = Path.Combine(webRoot, "uploads");

            if (!Directory.Exists(uploadsRoot))
            {
                return Task.FromResult<IReadOnlyList<MediaFileDto>>(Array.Empty<MediaFileDto>());
            }

            var searchDir = uploadsRoot;
            if (!string.IsNullOrWhiteSpace(query.Folder))
            {
                var safeFolder = Path.GetFileName(query.Folder.Trim());
                var folderPath = Path.Combine(uploadsRoot, safeFolder);
                if (Directory.Exists(folderPath))
                {
                    searchDir = folderPath;
                }
            }

            var dirInfo = new DirectoryInfo(searchDir);
            var fileInfos = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);

            var list = new List<MediaFileDto>();

            foreach (var fi in fileInfos)
            {
                var ext = fi.Extension.ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext)) continue;

                // Calculate relative path inside uploads
                var fullRelative = Path.GetRelativePath(webRoot, fi.FullName).Replace('\\', '/');
                var relativeUrl = "/" + fullRelative.TrimStart('/');

                var parts = fullRelative.Split('/');
                var folder = parts.Length > 2 ? parts[1] : "general";

                if (!string.IsNullOrWhiteSpace(query.SearchTerm) &&
                    !fi.Name.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                list.Add(new MediaFileDto
                {
                    FileName = fi.Name,
                    RelativeUrl = relativeUrl,
                    Folder = folder,
                    Extension = ext,
                    SizeBytes = fi.Length,
                    LastModified = fi.LastWriteTimeUtc
                });
            }

            var ordered = list
                .OrderByDescending(f => f.LastModified)
                .ToList();

            return Task.FromResult<IReadOnlyList<MediaFileDto>>(ordered);
        }

        public Task<IReadOnlyList<string>> GetFoldersAsync()
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var uploadsRoot = Path.Combine(webRoot, "uploads");

            if (!Directory.Exists(uploadsRoot))
            {
                return Task.FromResult<IReadOnlyList<string>>(new List<string> { "articles", "brands", "services", "projects", "team", "testimonials", "pages", "general" });
            }

            var dirs = Directory.GetDirectories(uploadsRoot)
                .Select(Path.GetFileName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!)
                .OrderBy(n => n)
                .ToList();

            var defaultFolders = new[] { "articles", "brands", "services", "projects", "team", "testimonials", "pages", "general" };
            foreach (var df in defaultFolders)
            {
                if (!dirs.Contains(df, StringComparer.OrdinalIgnoreCase))
                {
                    dirs.Add(df);
                }
            }

            return Task.FromResult<IReadOnlyList<string>>(dirs);
        }

        public async Task<ServiceResult<string>> UploadFileAsync(Stream stream, string fileName, string folder)
        {
            if (stream == null || stream.Length == 0)
            {
                return ServiceResult<string>.Invalid("File", "File cannot be empty.");
            }

            var safeFolder = string.IsNullOrWhiteSpace(folder) ? "general" : Path.GetFileName(folder.Trim());
            var url = await _storage.SaveFileAsync(stream, fileName, safeFolder);
            if (string.IsNullOrEmpty(url))
            {
                return ServiceResult<string>.Invalid("File", "Unsupported file format or storage error.");
            }

            return ServiceResult<string>.Success(url);
        }

        public async Task<ServiceResult> DeleteFileAsync(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return ServiceResult.Invalid("Path", "File path is required.");
            }

            // Security check: ensure path is within /uploads/
            var normalized = relativePath.Trim().Replace('\\', '/').TrimStart('/');
            if (!normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Invalid("Path", "Cannot delete files outside of uploads directory.");
            }

            _storage.DeleteFile(relativePath);

            var clean = relativePath.Trim().TrimStart('~').TrimStart('/').Replace('\\', '/');
            var fileName = Path.GetFileName(clean);

            try
            {
                // 1. Services (ImagePath)
                var services = await _db.CmsServices
                    .Where(s => !s.IsDeleted && s.ImagePath != null)
                    .ToListAsync();
                foreach (var s in services.Where(s => PathMatches(s.ImagePath, clean, fileName)))
                {
                    s.ImagePath = null;
                    s.UpdatedAt = DateTime.UtcNow;
                }

                // 2. Projects (ImagePath)
                var projects = await _db.CmsProjects
                    .Where(p => !p.IsDeleted && p.ImagePath != null)
                    .ToListAsync();
                foreach (var p in projects.Where(p => PathMatches(p.ImagePath, clean, fileName)))
                {
                    p.ImagePath = null;
                    p.UpdatedAt = DateTime.UtcNow;
                }

                // 3. Brands (LogoPath, LogoDarkPath, FaviconPath)
                var brands = await _db.BrandProfiles
                    .Where(b => !b.IsDeleted)
                    .ToListAsync();
                foreach (var b in brands)
                {
                    bool changed = false;
                    if (PathMatches(b.LogoPath, clean, fileName)) { b.LogoPath = null; changed = true; }
                    if (PathMatches(b.LogoDarkPath, clean, fileName)) { b.LogoDarkPath = null; changed = true; }
                    if (PathMatches(b.FaviconPath, clean, fileName)) { b.FaviconPath = null; changed = true; }
                    if (changed) b.UpdatedAt = DateTime.UtcNow;
                }

                // 4. Articles (CoverImagePath)
                var articles = await _db.Articles
                    .Where(a => a.CoverImagePath != null)
                    .ToListAsync();
                foreach (var a in articles.Where(a => PathMatches(a.CoverImagePath, clean, fileName)))
                {
                    a.CoverImagePath = null;
                    a.UpdatedAt = DateTime.UtcNow;
                }

                // 5. Team Members (PhotoPath)
                var team = await _db.TeamMembers
                    .Where(t => !t.IsDeleted && t.PhotoPath != null)
                    .ToListAsync();
                foreach (var t in team.Where(t => PathMatches(t.PhotoPath, clean, fileName)))
                {
                    t.PhotoPath = null;
                    t.UpdatedAt = DateTime.UtcNow;
                }

                // 6. Testimonials (PhotoPath)
                var testimonials = await _db.Testimonials
                    .Where(tm => !tm.IsDeleted && tm.PhotoPath != null)
                    .ToListAsync();
                foreach (var tm in testimonials.Where(tm => PathMatches(tm.PhotoPath, clean, fileName)))
                {
                    tm.PhotoPath = null;
                    tm.UpdatedAt = DateTime.UtcNow;
                }

                // 7. Page Sections (ValueEn, ValueAr)
                var sections = await _db.PageSections
                    .Where(sec => sec.DataType == "ImagePath")
                    .ToListAsync();
                foreach (var sec in sections)
                {
                    bool changed = false;
                    if (PathMatches(sec.ValueEn, clean, fileName)) { sec.ValueEn = string.Empty; changed = true; }
                    if (PathMatches(sec.ValueAr, clean, fileName)) { sec.ValueAr = string.Empty; changed = true; }
                    if (changed) sec.UpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking deleted media file from database entities: {Path}", relativePath);
            }

            return ServiceResult.Success();
        }

        private static bool PathMatches(string? value, string clean, string fileName)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var v = value.Trim().TrimStart('~').TrimStart('/').Replace('\\', '/');
            return v.Equals(clean, StringComparison.OrdinalIgnoreCase) ||
                   v.EndsWith("/" + clean, StringComparison.OrdinalIgnoreCase) ||
                   (!string.IsNullOrEmpty(fileName) && v.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<IReadOnlyList<MediaUsageDto>> GetFileUsagesAsync(string relativePath)
        {
            var usages = new List<MediaUsageDto>();

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return usages;
            }

            var clean = relativePath.Trim().TrimStart('~').TrimStart('/').Replace('\\', '/');
            var fileName = Path.GetFileName(clean);

            try
            {
                // 1. Brands (LogoPath, LogoDarkPath, FaviconPath)
                var brands = await _db.BrandProfiles.AsNoTracking()
                    .Where(b => !b.IsDeleted)
                    .ToListAsync();

                foreach (var b in brands)
                {
                    var brandName = !string.IsNullOrWhiteSpace(b.NameEn) ? b.NameEn : (!string.IsNullOrWhiteSpace(b.NameAr) ? b.NameAr : b.Slug);

                    if (PathMatches(b.LogoPath, clean, fileName))
                        usages.Add(new MediaUsageDto { EntityType = "Brand", EntityName = brandName, PropertyName = "Logo (Light)", EditUrl = $"/cms/brands/{b.Id}/edit" });

                    if (PathMatches(b.LogoDarkPath, clean, fileName))
                        usages.Add(new MediaUsageDto { EntityType = "Brand", EntityName = brandName, PropertyName = "Logo (Dark)", EditUrl = $"/cms/brands/{b.Id}/edit" });

                    if (PathMatches(b.FaviconPath, clean, fileName))
                        usages.Add(new MediaUsageDto { EntityType = "Brand", EntityName = brandName, PropertyName = "Favicon", EditUrl = $"/cms/brands/{b.Id}/edit" });
                }

                // 2. Articles (CoverImagePath, and embedded in BodyHtml)
                var articles = await _db.Articles.AsNoTracking().ToListAsync();
                foreach (var a in articles)
                {
                    var articleTitle = !string.IsNullOrWhiteSpace(a.TitleEn) ? a.TitleEn : a.TitleAr;

                    if (PathMatches(a.CoverImagePath, clean, fileName))
                        usages.Add(new MediaUsageDto { EntityType = "Article", EntityName = articleTitle, PropertyName = "Cover Image", EditUrl = $"/cms/articles/{a.Id}/edit" });

                    if ((!string.IsNullOrEmpty(a.BodyHtmlEn) && a.BodyHtmlEn.Contains(clean, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(a.BodyHtmlAr) && a.BodyHtmlAr.Contains(clean, StringComparison.OrdinalIgnoreCase)))
                    {
                        usages.Add(new MediaUsageDto { EntityType = "Article", EntityName = articleTitle, PropertyName = "Body Content (Embedded)", EditUrl = $"/cms/articles/{a.Id}/edit" });
                    }
                }

                // 3. Services (ImagePath)
                var services = await _db.CmsServices.AsNoTracking()
                    .Where(s => !s.IsDeleted)
                    .ToListAsync();

                foreach (var s in services)
                {
                    var serviceName = !string.IsNullOrWhiteSpace(s.NameEn) ? s.NameEn : s.NameAr;
                    if (PathMatches(s.ImagePath, clean, fileName))
                        usages.Add(new MediaUsageDto { EntityType = "Service", EntityName = serviceName, PropertyName = "Service Image", EditUrl = $"/cms/services/{s.Id}/edit" });
                }

                // 4. Projects (ImagePath)
                var projects = await _db.CmsProjects.AsNoTracking()
                    .Where(p => !p.IsDeleted)
                    .ToListAsync();

                foreach (var p in projects)
                {
                    var projectName = !string.IsNullOrWhiteSpace(p.NameEn) ? p.NameEn : p.NameAr;
                    if (PathMatches(p.ImagePath, clean, fileName))
                        usages.Add(new MediaUsageDto { EntityType = "Project", EntityName = projectName, PropertyName = "Project Image", EditUrl = $"/cms/projects/{p.Id}/edit" });
                }

                // 5. Team Members (PhotoPath)
                var team = await _db.TeamMembers.AsNoTracking()
                    .Where(t => !t.IsDeleted)
                    .ToListAsync();

                foreach (var t in team)
                {
                    var memberName = !string.IsNullOrWhiteSpace(t.NameEn) ? t.NameEn : t.NameAr;
                    if (PathMatches(t.PhotoPath, clean, fileName))
                        usages.Add(new MediaUsageDto { EntityType = "Team Member", EntityName = memberName, PropertyName = "Photo", EditUrl = $"/cms/team/{t.Id}/edit" });
                }

                // 6. Testimonials (PhotoPath)
                var testimonials = await _db.Testimonials.AsNoTracking()
                    .Where(tm => !tm.IsDeleted)
                    .ToListAsync();

                foreach (var tm in testimonials)
                {
                    var customerName = !string.IsNullOrWhiteSpace(tm.CustomerNameEn) ? tm.CustomerNameEn : tm.CustomerNameAr;
                    if (PathMatches(tm.PhotoPath, clean, fileName))
                        usages.Add(new MediaUsageDto { EntityType = "Testimonial", EntityName = customerName, PropertyName = "Customer Photo", EditUrl = $"/cms/testimonials/{tm.Id}/edit" });
                }

                // 7. Page Sections (ValueEn, ValueAr)
                var sections = await _db.PageSections.AsNoTracking().ToListAsync();
                foreach (var sec in sections)
                {
                    if (PathMatches(sec.ValueEn, clean, fileName) || PathMatches(sec.ValueAr, clean, fileName) ||
                        (!string.IsNullOrEmpty(sec.ValueEn) && sec.ValueEn.Contains(clean, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(sec.ValueAr) && sec.ValueAr.Contains(clean, StringComparison.OrdinalIgnoreCase)))
                    {
                        usages.Add(new MediaUsageDto
                        {
                            EntityType = "Page Section",
                            EntityName = $"Page: {sec.PageKey} | Section: {sec.SectionKey}",
                            PropertyName = sec.DataType == "ImagePath" ? "Section Image" : "Section Content",
                            EditUrl = $"/cms/pages"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting media usages for file path: {Path}", relativePath);
            }

            return usages;
        }
    }
}
