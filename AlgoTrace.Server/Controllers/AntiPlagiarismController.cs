using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrace.Server.Controllers
{
    [ApiController]
    [Route("api/analysis")]
    public class AnalysisController : ControllerBase
    {
        private readonly ITextAnalysisService _textService;
        private readonly ITokenAnalysisService _tokenService;
        private readonly ITreeAnalysisService _treeService;
        private readonly IGraphAnalysisService _graphService;
        private readonly IMetricAnalysisService _metricService;
        private readonly IUnifiedAnalysisService _unifiedService;

        public AnalysisController(
            ITextAnalysisService textService,
            ITokenAnalysisService tokenService,
            ITreeAnalysisService treeService,
            IGraphAnalysisService graphService,
            IMetricAnalysisService metricService,
            IUnifiedAnalysisService unifiedService
        )
        {
            _textService = textService;
            _tokenService = tokenService;
            _treeService = treeService;
            _graphService = graphService;
            _metricService = metricService;
            _unifiedService = unifiedService;
        }

        [HttpPost("unified")]
        public IActionResult CompareUnified([FromBody] UnifiedAnalysisRequest request)
        {
            var result = _unifiedService.Analyze(request);
            return Ok(result);
        }

        [HttpPost("text/compare")]
        public IActionResult CompareText([FromBody] AnalysisRequest request)
        {
            var result = _textService.Analyze(request);
            return Ok(result);
        }

        [HttpPost("token/compare")]
        public IActionResult CompareTokens([FromBody] AnalysisRequest request)
        {
            var result = _tokenService.Analyze(request);
            return Ok(result);
        }

        [HttpPost("tree/compare")]
        public IActionResult CompareTree([FromBody] AnalysisRequest request)
        {
            var result = _treeService.Analyze(request);
            return Ok(result);
        }

        [HttpPost("graph/compare")]
        public IActionResult CompareGraph([FromBody] AnalysisRequest request)
        {
            var result = _graphService.Analyze(request);
            return Ok(result);
        }

        [HttpPost("metric/compare")]
        public IActionResult CompareMetric([FromBody] AnalysisRequest request)
        {
            var result = _metricService.Analyze(request);
            return Ok(result);
        }
    }
}
