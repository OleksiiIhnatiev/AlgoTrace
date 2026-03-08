using AlgoTrace.Server.Interfaces;
using Microsoft.AspNetCore.Mvc;
using AlgoTrace.Server.Services;
using Microsoft.AspNetCore.Authorization;

namespace AlgoTrace.Server.Controllers
{
    [ApiController]
    [Route("api/submission")]
    [Authorize]
    public class SubmissionController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;

        public SubmissionController(ISubmissionService submissionService)
        {
            _submissionService = submissionService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadSubmission(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty or not provided.");
            }

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _submissionService.ProcessSubmissionAsync(stream, file.FileName);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
