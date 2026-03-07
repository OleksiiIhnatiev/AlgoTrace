using Microsoft.AspNetCore.Mvc;
using AlgoTrace.Server.Services;
using AlgoTrace.Server.Interfaces;

namespace AlgoTrace.Server.Controllers
{
    [ApiController]
    [Route("api/analysis/text")]
    public class TextAnalysisController : ControllerBase
    {
        private readonly ITextAnalysisService _service;

        public TextAnalysisController(ITextAnalysisService service)
        {
            _service = service;
        }

        [HttpPost("compare")]
        public IActionResult Compare([FromBody] TextCompareRequest request)
        {
            var result = _service.Analyze(
                request.SourceCode,
                request.ReferenceCode,
                request.SourceName,
                request.RefName
            );
            return Ok(result);
        }
    }

    public class TextCompareRequest
    {
        public string SourceCode { get; set; } = "";
        public string ReferenceCode { get; set; } = "";
        public string SourceName { get; set; } = "input_file.py";
        public string RefName { get; set; } = "reference_file.py";
    }
}
