using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO.Directory;
using AlgoTrace.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AlgoTrace.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DirectoryController : ControllerBase
    {
        private readonly IDirectoryService _directoryService;

        public DirectoryController(IDirectoryService directoryService)
        {
            _directoryService = directoryService;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        #region Folder Operations

        [HttpGet("all-folders")]
        public async Task<IActionResult> GetAllUserFolders()
        {
            var folders = await _directoryService.GetAllFoldersAsync(GetUserId());
            return Ok(folders);
        }

        [HttpGet("folder/{folderId?}")]
        public async Task<IActionResult> GetFolder(Guid? folderId)
        {
            var result = await _directoryService.GetFolderContentAsync(folderId, GetUserId());
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost("folder")]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderRequest model)
        {
            var newFolder = await _directoryService.CreateFolderAsync(model, GetUserId());
            return Ok(newFolder);
        }

        [HttpPut("folder/{id}/rename")]
        public async Task<IActionResult> RenameFolder(Guid id, [FromBody] string newName)
        {
            var success = await _directoryService.RenameFolderAsync(id, newName, GetUserId());
            if (!success)
                return NotFound();

            return Ok();
        }

        [HttpDelete("folder/{id}")]
        public async Task<IActionResult> DeleteFolder(Guid id)
        {
            var success = await _directoryService.DeleteFolderAsync(id, GetUserId());
            if (!success)
                return NotFound();

            return Ok();
        }

        #endregion

        #region File Operations

        [HttpPost("file/upload")]
        public async Task<IActionResult> UploadFiles([FromForm] List<IFormFile> files, [FromQuery] Guid? folderId)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest("Не вибрано жодного файлу для завантаження.");
            }

            var fileEntries = await _directoryService.UploadFilesAsync(files, folderId, GetUserId());
            return Ok(fileEntries);
        }

        [HttpGet("file/download/{fileId}")]
        public async Task<IActionResult> DownloadFile(Guid fileId)
        {
            var result = await _directoryService.DownloadFileAsync(fileId, GetUserId());
            if (result == null)
                return NotFound("Файл не знайдено або видалено з сервера.");

            return File(result.Bytes, result.ContentType, result.FileName);
        }

        [HttpDelete("file/{fileId}")]
        public async Task<IActionResult> DeleteFile(Guid fileId)
        {
            var success = await _directoryService.DeleteFileAsync(fileId, GetUserId());
            if (!success)
                return NotFound();

            return Ok();
        }

        #endregion
    }
}
