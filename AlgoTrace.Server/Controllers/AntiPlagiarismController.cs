﻿using AlgoTrace.Server.Interfaces;
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
        private readonly IGraphAnalysisService _graphService;
        private readonly IMetricAnalysisService _metricService;

        public AnalysisController(
            ITextAnalysisService textService,
            ITokenAnalysisService tokenService,
            IGraphAnalysisService graphService,
            IMetricAnalysisService metricService
        )
        {
            _textService = textService;
            _tokenService = tokenService;
            _graphService = graphService;
            _metricService = metricService;
        }

        [HttpPost("text/compare")]
        public IActionResult CompareText([FromBody] TextCompareRequest request)
        {
            var result = _textService.Analyze(
                request.SourceCode,
                request.ReferenceCode,
                request.SourceName,
                request.RefName
            );
            return Ok(result);
        }

        [HttpPost("token/compare")]
        public IActionResult CompareTokens([FromBody] TextCompareRequest request)
        {
            var result = _tokenService.Analyze(
                request.SourceCode,
                request.ReferenceCode,
                request.SourceName,
                request.RefName
            );
            return Ok(result);
        }

        [HttpPost("graph/compare")]
        public IActionResult CompareGraph([FromBody] GraphAnalysisRequest request)
        {
            var result = _graphService.Analyze(request);
            return Ok(result);
        }

        [HttpPost("metric/compare")]
        public IActionResult CompareMetric([FromBody] MetricAnalysisRequest request)
        {
            var result = _metricService.Analyze(request);
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
