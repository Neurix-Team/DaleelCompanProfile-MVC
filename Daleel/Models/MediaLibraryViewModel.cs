using Daleel.BAL.Models;
using Microsoft.AspNetCore.Http;

namespace Daleel.Models
{
    public class MediaLibraryViewModel
    {
        public IReadOnlyList<MediaFileDto> Files { get; set; } = Array.Empty<MediaFileDto>();

        public IReadOnlyList<string> Folders { get; set; } = Array.Empty<string>();

        public string? SelectedFolder { get; set; }

        public string? SearchTerm { get; set; }

        public long TotalSizeBytes => Files.Sum(f => f.SizeBytes);

        public string TotalSizeFormatted
        {
            get
            {
                if (TotalSizeBytes < 1024) return $"{TotalSizeBytes} B";
                if (TotalSizeBytes < 1024 * 1024) return $"{TotalSizeBytes / 1024.0:F1} KB";
                return $"{TotalSizeBytes / (1024.0 * 1024.0):F2} MB";
            }
        }
    }

    public class MediaUploadViewModel
    {
        public IFormFile? File { get; set; }

        public string Folder { get; set; } = "general";
    }
}
