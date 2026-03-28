using AlgoTrace.Server.Models;
using AlgoTrace.Server.Models.DTO.Directory;

namespace AlgoTrace.Server.Interfaces
{
    public interface IDirectoryService
    {
        Task<IEnumerable<FolderDto>> GetAllFoldersAsync(string userId);
        Task<FolderContentDto?> GetFolderContentAsync(Guid? folderId, string userId);
        Task<Folder> CreateFolderAsync(CreateFolderRequest model, string userId);
        Task<bool> RenameFolderAsync(Guid folderId, string newName, string userId);
        Task<bool> DeleteFolderAsync(Guid folderId, string userId);

        Task<IEnumerable<SourceFile>> UploadFilesAsync(
            IEnumerable<IFormFile> files,
            Guid? folderId,
            string userId
        );
        Task<FileDownloadResult?> DownloadFileAsync(Guid fileId, string userId);
        Task<bool> DeleteFileAsync(Guid fileId, string userId);
    }
}
