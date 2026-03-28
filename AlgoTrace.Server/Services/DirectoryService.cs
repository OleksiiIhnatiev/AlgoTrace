using AlgoTrace.Server.Data;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models;
using AlgoTrace.Server.Models.DTO.Directory;
using Microsoft.EntityFrameworkCore;

namespace AlgoTrace.Server.Services
{
    public class DirectoryService : IDirectoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _storagePath;

        public DirectoryService(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _storagePath = Path.Combine(env.ContentRootPath, "Storage");
            if (!Directory.Exists(_storagePath))
                Directory.CreateDirectory(_storagePath);
        }

        public async Task<IEnumerable<FolderDto>> GetAllFoldersAsync(string userId)
        {
            return await _context
                .Folders.Where(f => f.UserId == userId)
                .Select(f => new FolderDto
                {
                    FolderId = f.FolderId,
                    Name = f.Name,
                    ParentId = f.ParentId,
                })
                .ToListAsync();
        }

        public async Task<FolderContentDto?> GetFolderContentAsync(Guid? folderId, string userId)
        {
            if (folderId == null)
            {
                var rootFolders = await _context
                    .Folders.Where(f => f.UserId == userId && f.ParentId == null)
                    .Select(f => new FolderDto { FolderId = f.FolderId, Name = f.Name })
                    .ToListAsync();

                var rootFiles = await _context
                    .SourceFiles.Where(f => f.UserId == userId && f.FolderId == null)
                    .Select(f => new FileDto { FileId = f.FileId, Name = f.Name })
                    .ToListAsync();

                return new FolderContentDto
                {
                    FolderId = null,
                    Name = "Мій диск",
                    Folders = rootFolders,
                    Files = rootFiles,
                };
            }

            var folder = await _context
                .Folders.Include(f => f.SubFolders)
                .Include(f => f.Files)
                .FirstOrDefaultAsync(f => f.UserId == userId && f.FolderId == folderId);

            if (folder == null)
                return null;

            return new FolderContentDto
            {
                FolderId = folder.FolderId,
                Name = folder.Name,
                ParentId = folder.ParentId,
                Folders = folder.SubFolders.Select(s => new FolderDto
                {
                    FolderId = s.FolderId,
                    Name = s.Name,
                }),
                Files = folder.Files.Select(f => new FileDto { FileId = f.FileId, Name = f.Name }),
            };
        }

        public async Task<Folder> CreateFolderAsync(CreateFolderRequest model, string userId)
        {
            var newFolder = new Folder
            {
                Name = model.Name,
                ParentId = model.ParentId,
                UserId = userId,
            };
            _context.Folders.Add(newFolder);
            await _context.SaveChangesAsync();
            return newFolder;
        }

        public async Task<bool> RenameFolderAsync(Guid folderId, string newName, string userId)
        {
            var folder = await _context.Folders.FirstOrDefaultAsync(f =>
                f.FolderId == folderId && f.UserId == userId
            );
            if (folder == null)
                return false;

            folder.Name = newName;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteFolderAsync(Guid folderId, string userId)
        {
            var folder = await _context
                .Folders.Include(f => f.SubFolders)
                .Include(f => f.Files)
                .FirstOrDefaultAsync(f => f.FolderId == folderId && f.UserId == userId);

            if (folder == null)
                return false;

            await DeleteFolderContentsRecursive(folder);
            _context.Folders.Remove(folder);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task DeleteFolderContentsRecursive(Folder folder)
        {
            var files = await _context
                .SourceFiles.Where(f => f.FolderId == folder.FolderId)
                .ToListAsync();

            foreach (var file in files)
            {
                var fullPath = Path.Combine(_storagePath, file.Path);
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);

                _context.SourceFiles.Remove(file);
            }

            var subFolders = await _context
                .Folders.Where(f => f.ParentId == folder.FolderId)
                .ToListAsync();

            foreach (var sub in subFolders)
            {
                await DeleteFolderContentsRecursive(sub);
                _context.Folders.Remove(sub);
            }
        }

        public async Task<IEnumerable<SourceFile>> UploadFilesAsync(
            IEnumerable<IFormFile> files,
            Guid? folderId,
            string userId
        )
        {
            var uploadedFiles = new List<SourceFile>();

            foreach (var file in files)
            {
                if (file.Length == 0)
                    continue;

                var uniqueName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(_storagePath, uniqueName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var fileEntry = new SourceFile
                {
                    Name = file.FileName,
                    Path = uniqueName,
                    UserId = userId,
                    FolderId = folderId,
                };

                _context.SourceFiles.Add(fileEntry);
                uploadedFiles.Add(fileEntry);
            }

            await _context.SaveChangesAsync();

            return uploadedFiles;
        }

        public async Task<FileDownloadResult?> DownloadFileAsync(Guid fileId, string userId)
        {
            var file = await _context.SourceFiles.FirstOrDefaultAsync(f =>
                f.FileId == fileId && f.UserId == userId
            );
            if (file == null)
                return null;

            var filePath = Path.Combine(_storagePath, file.Path);
            if (!System.IO.File.Exists(filePath))
                return null;

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return new FileDownloadResult
            {
                Bytes = bytes,
                ContentType = "application/octet-stream",
                FileName = file.Name,
            };
        }

        public async Task<bool> DeleteFileAsync(Guid fileId, string userId)
        {
            var file = await _context.SourceFiles.FirstOrDefaultAsync(f =>
                f.FileId == fileId && f.UserId == userId
            );
            if (file == null)
                return false;

            var filePath = Path.Combine(_storagePath, file.Path);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            _context.SourceFiles.Remove(file);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
