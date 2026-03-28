using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AlgoTrace.Server.Data;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO.Analysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlgoTrace.Server.Controllers
{
    [ApiController]
    [Route("api/analysis")]
    public class AnalysisController : ControllerBase
    {
        private readonly IUnifiedAnalysisService _unifiedService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnalysisController> _logger;

        public AnalysisController(
            IUnifiedAnalysisService unifiedService,
            ApplicationDbContext context,
            ILogger<AnalysisController> logger
        )
        {
            _unifiedService = unifiedService;
            _context = context;
            _logger = logger;
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
                try
                {
                    if (!Guid.TryParse(docId, out var fileId))
                    {
                        var errorMessage = $"Неверный формат идентификатора документа: {docId}";
                        _logger.LogWarning(errorMessage);
                        multiResponse.Results.Add(
                            new DocumentComparisonResult
                            {
                                DocumentId = docId,
                                Error = errorMessage,
                            }
                        );
                        continue;
                    }

                    var targetFile = await _context
                        .SourceFiles.AsNoTracking()
                        .FirstOrDefaultAsync(f => f.FileId == fileId);

                    if (targetFile == null)
                    {
                        var errorMessage = $"Документ с ID {docId} не найден в базе данных.";
                        _logger.LogWarning(errorMessage);
                        multiResponse.Results.Add(
                            new DocumentComparisonResult
                            {
                                DocumentId = docId,
                                Error = errorMessage,
                            }
                        );
                        continue;
                    }

                    var storageFolder = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
                    var fullFilePath = Path.Combine(storageFolder, targetFile.Path);

                    if (!System.IO.File.Exists(fullFilePath))
                    {
                        var errorMessage =
                            $"Файл документа отсутствует на диске. Путь: {fullFilePath}";
                        _logger.LogError(errorMessage);
                        multiResponse.Results.Add(
                            new DocumentComparisonResult
                            {
                                DocumentId = docId,
                                Error = errorMessage,
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
                                new CodeFile
                                {
                                    Filename = targetFile.Name,
                                    Content = targetContent,
                                },
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
                catch (Exception ex)
                {
                    var errorMessage =
                        $"Произошла непредвиденная ошибка при обработке документа {docId}.";
                    _logger.LogError(ex, errorMessage);
                    multiResponse.Results.Add(
                        new DocumentComparisonResult
                        {
                            DocumentId = docId,
                            Error = $"{errorMessage} Детали: {ex.Message}",
                        }
                    );
                }
            }

            return Ok(multiResponse);
        }
    }
}
