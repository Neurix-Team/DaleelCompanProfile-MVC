namespace Daleel.BAL.Services.Interfaces
{
    public interface IFileStorageService
    {
        Task<string?> SaveFileAsync(Stream fileStream, string fileName, string folder = "articles");
        void DeleteFile(string relativePath);
    }
}
