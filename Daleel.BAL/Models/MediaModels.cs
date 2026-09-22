namespace Daleel.BAL.Models
{
    public class MediaFileDto
    {
        public string FileName { get; set; } = string.Empty;

        public string RelativeUrl { get; set; } = string.Empty;

        public string Folder { get; set; } = string.Empty;

        public string Extension { get; set; } = string.Empty;

        public long SizeBytes { get; set; }

        public string SizeFormatted
        {
            get
            {
                if (SizeBytes < 1024) return $"{SizeBytes} B";
                if (SizeBytes < 1024 * 1024) return $"{SizeBytes / 1024.0:F1} KB";
                return $"{SizeBytes / (1024.0 * 1024.0):F2} MB";
            }
        }

        public bool IsImage => Extension is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".svg";

        public DateTime LastModified { get; set; }
    }

    public class MediaLibraryQuery
    {
        public string? Folder { get; set; }

        public string? SearchTerm { get; set; }

        public string? FileType { get; set; }
    }

    public class MediaUsageDto
    {
        public string EntityType { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;

        public string PropertyName { get; set; } = string.Empty;

        public string? EditUrl { get; set; }
    }
}
