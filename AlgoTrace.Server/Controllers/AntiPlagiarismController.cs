using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AlgoTrace.Server.Data;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlgoTrace.Server.Controllers
{
    [ApiController]
    [Route("api/analysis")]
    public class AnalysisController : ControllerBase
    {
        private readonly IUnifiedAnalysisService _unifiedService;
        private readonly ApplicationDbContext _context;

        public AnalysisController(
            IUnifiedAnalysisService unifiedService,
            ApplicationDbContext context
        )
        {
            _unifiedService = unifiedService;
            _context = context;
        }

        [HttpPost("unified")]
        public IActionResult CompareUnified([FromBody] UnifiedAnalysisRequest request)
        {
            var result = _unifiedService.Analyze(request);
            return Ok(result);
        }

        [HttpPost("compare-multiple")]
        public async Task<IActionResult> CompareWithMultipleDocuments(
            [FromBody] MultiDocumentAnalysisRequest request
        )
        {
            if (
                request?.Submission?.Files == null
                || !request.Submission.Files.Any()
                || request.CompareWithDocumentIds == null
                || !request.CompareWithDocumentIds.Any()
            )
            {
                return BadRequest("Invalid request. Submission and document IDs are required.");
            }

            var mainFile = request.Submission.Files.First();
            var multiResponse = new MultiDocumentAnalysisResponse
            {
                AnalysisId = $"req_multi_{System.Guid.NewGuid().ToString().Substring(0, 8)}",
                MainSubmission = new CodeFile
                {
                    Filename = mainFile.Filename,
                    Content = mainFile.Content,
                },
                Results = new List<DocumentComparisonResult>(),
            };

            var executeCategories = new ExecuteCategories();
            if (request.AnalysisConfig?.Categories != null)
            {
                foreach (var category in request.AnalysisConfig.Categories)
                {
                    switch (category.CategoryName)
                    {
                        case "text_analysis":
                        case "text_based":
                            executeCategories.TextBased = category.Methods;
                            break;
                        case "token_analysis":
                        case "token_based":
                            executeCategories.TokenBased = category.Methods;
                            break;
                        case "tree_analysis":
                        case "tree_based":
                            executeCategories.TreeBased = category.Methods;
                            break;
                        case "structural_analysis":
                        case "graph_analysis":
                        case "graph_based":
                            executeCategories.GraphBased = category.Methods;
                            break;
                        case "metric_analysis":
                        case "metrics_based":
                            executeCategories.MetricsBased = category.Methods;
                            break;
                    }
                }
            }

            foreach (var docId in request.CompareWithDocumentIds)
            {
                if (!System.Guid.TryParse(docId, out var fileId))
                {
                    multiResponse.Results.Add(
                        new DocumentComparisonResult
                        {
                            DocumentId = docId,
                            Error = "invalid_document_id",
                        }
                    );
                    continue;
                }

                var targetFile = await _context
                    .SourceFiles.AsNoTracking()
                    .FirstOrDefaultAsync(f => f.FileId == fileId);

                if (targetFile == null)
                {
                    multiResponse.Results.Add(
                        new DocumentComparisonResult
                        {
                            DocumentId = docId,
                            Error = "document_not_found_in_db",
                        }
                    );
                    continue;
                }

                var storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
                var fullFilePath = Path.Combine(storageFolder, targetFile.Path);

                if (!System.IO.File.Exists(fullFilePath))
                {
                    multiResponse.Results.Add(
                        new DocumentComparisonResult
                        {
                            DocumentId = docId,
                            Error = "document_file_missing_on_disk",
                        }
                    );
                    continue;
                }

                var targetContent = await System.IO.File.ReadAllTextAsync(fullFilePath);

                var unifiedRequest = new UnifiedAnalysisRequest
                {
                    Language = request.Language,
                    SubmissionA = request.Submission,
                    SubmissionB = new SubmissionData
                    {
                        Files = new List<CodeFile>
                        {
                            new CodeFile { Filename = targetFile.Name, Content = targetContent },
                        },
                    },
                    AnalysisConfig = new UnifiedConfig
                    {
                        Parameters = request.AnalysisConfig.Parameters,
                        ExecuteCategories = executeCategories,
                    },
                };

                var unifiedResult = _unifiedService.Analyze(unifiedRequest);

                multiResponse.Results.Add(
                    new DocumentComparisonResult
                    {
                        DocumentId = docId,
                        TargetFile = new CodeFile
                        {
                            Filename = targetFile.Name,
                            Content = targetContent,
                        },
                        GlobalSimilarityScore = unifiedResult.GlobalSimilarityScore,
                        CategoriesResults = unifiedResult.CategoriesResults,
                    }
                );
            }

            return Ok(multiResponse);
        }
    }
}
