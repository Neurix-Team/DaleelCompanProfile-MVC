using Daleel.BAL.Models;

namespace Daleel.BAL.Services.Interfaces
{
    public interface IMediaLibraryService
    {
        Task<IReadOnlyList<MediaFileDto>> GetFilesAsync(MediaLibraryQuery query);

        Task<IReadOnlyList<string>> GetFoldersAsync();

        Task<ServiceResult<string>> UploadFileAsync(Stream stream, string fileName, string folder);

        Task<ServiceResult> DeleteFileAsync(string relativePath);

        Task<IReadOnlyList<MediaUsageDto>> GetFileUsagesAsync(string relativePath);
    }
}
