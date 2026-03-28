using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using AlgoTrace.Server.Algorithms.Graph;
using AlgoTrace.Server.Algorithms.Metric;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO.Analysis;
using AlgoTrace.Server.ParserFactory;
using AlgoTrace.Server.Utils;
using static AlgoTrace.Server.Algorithms.Graph.GraphUtils;

namespace AlgoTrace.Server.Services
{
    public class UnifiedAnalysisService : IUnifiedAnalysisService
    {
        private readonly IEnumerable<ITextAlgorithm> _textAlgos;
        private readonly IEnumerable<ITokenAlgorithm> _tokenAlgos;
        private readonly IEnumerable<ITreeAlgorithm> _treeAlgos;
        private readonly IEnumerable<IGraphAlgorithm> _graphAlgos;
        private readonly IEnumerable<IMetricAlgorithm> _metricAlgos;
        private readonly ParserFactory.ParserFactory _parserFactory;

        public UnifiedAnalysisService(
            IEnumerable<ITextAlgorithm> textAlgos,
            IEnumerable<ITokenAlgorithm> tokenAlgos,
            IEnumerable<ITreeAlgorithm> treeAlgos,
            IEnumerable<IGraphAlgorithm> graphAlgos,
            IEnumerable<IMetricAlgorithm> metricAlgos,
            ParserFactory.ParserFactory parserFactory
        )
        {
            _textAlgos = textAlgos;
            _tokenAlgos = tokenAlgos;
            _treeAlgos = treeAlgos;
            _graphAlgos = graphAlgos;
            _metricAlgos = metricAlgos;
            _parserFactory = parserFactory;
        }

        public UnifiedAnalysisResponse Analyze(UnifiedAnalysisRequest request)
        {
            var response = new UnifiedAnalysisResponse
            {
                AnalysisId = $"req_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Status = "completed",
                CategoriesResults = new List<CategoryResult>(),
            };

            var fileA = request.SubmissionA.Files.FirstOrDefault();
            var fileB = request.SubmissionB.Files.FirstOrDefault();

            if (fileA == null || fileB == null)
                return response;

            response.SourceFiles = new SourceFilesInfo
            {
                FileA = fileA.Content,
                FileB = fileB.Content,
                NameA = fileA.Filename ?? "Файл 1",
                NameB = fileB.Filename ?? "Файл 2",
            };

            string contentA = fileA.Content;
            string contentB = fileB.Content;
            var parameters = request.AnalysisConfig.Parameters;
            var categories = request.AnalysisConfig.ExecuteCategories;

            if (categories.TextBased != null && categories.TextBased.Any())
            {
                var result = ProcessText(contentA, contentB, categories.TextBased, fileA.Filename, fileB.Filename);
                response.CategoriesResults.Add(result);
            }

            if (categories.TokenBased != null && categories.TokenBased.Any())
            {
                var result = ProcessToken(contentA, contentB, categories.TokenBased);
                response.CategoriesResults.Add(result);
            }

            if (categories.TreeBased != null && categories.TreeBased.Any())
            {
                var result = ProcessTree(contentA, contentB, request.Language, categories.TreeBased, parameters);
                response.CategoriesResults.Add(result);
            }

            if (categories.GraphBased != null && categories.GraphBased.Any())
            {
                var result = ProcessGraph(contentA, contentB, request.Language, categories.GraphBased, parameters);
                response.CategoriesResults.Add(result);
            }

            if (categories.MetricsBased != null && categories.MetricsBased.Any())
            {
                var result = ProcessMetrics(contentA, contentB, categories.MetricsBased, parameters);
                response.CategoriesResults.Add(result);
            }

            if (response.CategoriesResults.Any())
            {
                response.GlobalSimilarityScore = Math.Round(
                    response.CategoriesResults.Average(c => c.CategorySimilarityScore),
                    2
                );
            }

            return response;
        }

        private CategoryResult ProcessText(string a, string b, List<string> methods, string nameA, string nameB)
        {
            var categoryResult = new CategoryResult { CategoryName = "text_based" };
            double totalScore = 0;
            int count = 0;
            var sLinesA = SourceNormalizer.GetLines(a);
            var sLinesB = SourceNormalizer.GetLines(b);

            foreach (var algo in _textAlgos)
            {
                if (!methods.Contains(algo.Key)) continue;

                var matches = algo.Execute(a, b, out double score);
                totalScore += score / 100.0;
                count++;

                var evidence = new TextEvidence();
                foreach (var m in matches)
                {
                    int startA = m.LeftLines.FirstOrDefault();
                    int endA = m.LeftLines.LastOrDefault();
                    int startB = m.RightLines.FirstOrDefault();
                    int endB = m.RightLines.LastOrDefault();

                    evidence.MatchedBlocks.Add(new
                    {
                        file_a = nameA,
                        file_b = nameB,
                        start_line_a = startA,
                        end_line_a = endA,
                        start_line_b = startB,
                        end_line_b = endB,
                        content_a = startA > 0 && startA <= sLinesA.Length ? string.Join("\n", sLinesA.Skip(startA - 1).Take(Math.Max(1, endA - startA + 1))) : "",
                        content_b = startB > 0 && startB <= sLinesB.Length ? string.Join("\n", sLinesB.Skip(startB - 1).Take(Math.Max(1, endB - startB + 1))) : ""
                    });
                }

                categoryResult.Algorithms.Add(new AlgorithmResult
                {
                    Method = algo.Key,
                    SimilarityScore = Math.Round(score / 100.0, 2),
                    EvidenceType = "text_highlight",
                    Evidence = evidence
                });
            }
            categoryResult.CategorySimilarityScore = count > 0 ? Math.Round(totalScore / count, 2) : 0;
            return categoryResult;
        }

        private CategoryResult ProcessToken(string a, string b, List<string> methods)
        {
            var categoryResult = new CategoryResult { CategoryName = "token_based" };
            var tokensA = Tokenize(a);
            var tokensB = Tokenize(b);
            double totalScore = 0;
            int count = 0;

            foreach (var algo in _tokenAlgos)
            {
                if (!methods.Contains(algo.Key)) continue;

                var matches = algo.Execute(tokensA, tokensB, out double score);
                totalScore += score / 100.0;
                count++;

                var evidence = new TokenEvidence();
                foreach (var match in matches)
                {
                    evidence.MatchedHashes.Add(new
                    {
                        hash_value = match.Id.ToString(),
                        token_sequence = match.Type,
                        occurrences = new[]
                        {
                            new { submission = "a", token_start_index = match.LeftLines.FirstOrDefault(), token_end_index = match.LeftLines.LastOrDefault() },
                            new { submission = "b", token_start_index = match.RightLines.FirstOrDefault(), token_end_index = match.RightLines.LastOrDefault() }
                        }
                    });
                }

                categoryResult.Algorithms.Add(new AlgorithmResult
                {
                    Method = algo.Key,
                    SimilarityScore = Math.Round(score / 100.0, 2),
                    EvidenceType = "token_sequence",
                    Evidence = evidence
                });
            }
            categoryResult.CategorySimilarityScore = count > 0 ? Math.Round(totalScore / count, 2) : 0;
            return categoryResult;
        }

        private CategoryResult ProcessTree(string a, string b, string lang, List<string> methods, Dictionary<string, object> parameters)
        {
            var categoryResult = new CategoryResult { CategoryName = "tree_based" };

            try
            {
                var parser = _parserFactory.GetParser(lang);
                var treeA = parser.Parse(a);
                var treeB = parser.Parse(b);
                double totalScore = 0;
                int count = 0;

                foreach (var algo in _treeAlgos)
                {
                    if (!methods.Contains(algo.Key)) continue;

                    double score = algo.Calculate(treeA, treeB, parameters, out object evidence);
                    totalScore += score / 100.0;
                    count++;

                    categoryResult.Algorithms.Add(new AlgorithmResult
                    {
                        Method = algo.Key,
                        SimilarityScore = Math.Round(score / 100.0, 2),
                        EvidenceType = "ast_tree_mapping",
                        Evidence = evidence
                    });
                }
                categoryResult.CategorySimilarityScore = count > 0 ? Math.Round(totalScore / count, 2) : 0;
            }
            catch (NotSupportedException) { }

            return categoryResult;
        }

        private CategoryResult ProcessGraph(string a, string b, string lang, List<string> methods, Dictionary<string, object> parameters)
        {
            var categoryResult = new CategoryResult { CategoryName = "graph_based" };

            try
            {
                var parser = _parserFactory.GetParser(lang);
                var treeA = parser.Parse(a);
                var treeB = parser.Parse(b);
                double totalScore = 0;
                int count = 0;

                foreach (var algo in _graphAlgos)
                {
                    if (!methods.Contains(algo.Key)) continue;

                    var matches = algo.Execute(treeA, treeB, parameters, out double score, out CodeGraph graphA, out CodeGraph graphB);
                    totalScore += score / 100.0;
                    count++;

                    categoryResult.Algorithms.Add(new AlgorithmResult
                    {
                        Method = algo.Key,
                        SimilarityScore = Math.Round(score / 100.0, 2),
                        EvidenceType = "graph_mapping",
                        Evidence = new
                        {
                            graph_a = new { nodes = graphA.Nodes.Select(n => new { id = n.Id, line = n.LineIndex + 1, content = n.Content, type = n.Type, variables = n.Variables }), edges = graphA.Edges.Select(e => new { source = e.SourceId, target = e.TargetId, type = e.Type }) },
                            graph_b = new { nodes = graphB.Nodes.Select(n => new { id = n.Id, line = n.LineIndex + 1, content = n.Content, type = n.Type, variables = n.Variables }), edges = graphB.Edges.Select(e => new { source = e.SourceId, target = e.TargetId, type = e.Type }) },
                            matches = matches.Select(m => new { id = m.Id, type = m.Type, severity = m.Severity, left_lines = m.LeftLines, right_lines = m.RightLines }).ToList()
                        }
                    });
                }
                categoryResult.CategorySimilarityScore = count > 0 ? Math.Round(totalScore / count, 2) : 0;
            }
            catch (NotSupportedException) { }

            return categoryResult;
        }

        private CategoryResult ProcessMetrics(string a, string b, List<string> methods, Dictionary<string, object> parameters)
        {
            var categoryResult = new CategoryResult { CategoryName = "metrics_based" };
            double totalScore = 0;
            int count = 0;

            foreach (var algo in _metricAlgos)
            {
                if (!methods.Contains(algo.Key)) continue;

                var matches = algo.Execute(a, b, parameters, out double score);
                totalScore += score / 100.0;
                count++;

                var evidence = new MetricEvidence { Conclusion = $"Схожість метрик: {score:F1}%" };
                if (algo.Key == "halstead")
                {
                    var hA = MetricUtils.CalculateHalsteadMetrics(a);
                    var hB = MetricUtils.CalculateHalsteadMetrics(b);
                    evidence.MetricsA = new Dictionary<string, double> { { "halstead_volume", hA.Volume }, { "halstead_difficulty", hA.Difficulty } };
                    evidence.MetricsB = new Dictionary<string, double> { { "halstead_volume", hB.Volume }, { "halstead_difficulty", hB.Difficulty } };
                }
                else if (algo.Key == "mccabe")
                {
                    var cA = MetricUtils.CalculateMcCabeComplexity(a);
                    var cB = MetricUtils.CalculateMcCabeComplexity(b);
                    evidence.MetricsA = new Dictionary<string, double> { { "cyclomatic_complexity", cA } };
                    evidence.MetricsB = new Dictionary<string, double> { { "cyclomatic_complexity", cB } };
                }

                categoryResult.Algorithms.Add(new AlgorithmResult
                {
                    Method = algo.Key,
                    SimilarityScore = Math.Round(score / 100.0, 2),
                    EvidenceType = "metric_comparison",
                    Evidence = evidence
                });
            }
            categoryResult.CategorySimilarityScore = count > 0 ? Math.Round(totalScore / count, 2) : 0;
            return categoryResult;
        }

        private List<TokenInfo> Tokenize(string code)
        {
            var tokens = new List<TokenInfo>();
            
            var pattern = @"(@""(?:[^""]|"""")*""|""(?:\\.|[^\\""])*""|'(?:\\.|[^\\'])*'|`(?:\\.|[^\\`])*`|//.*?$|/\*[\s\S]*?\*/|#.*?$)";
            string normalized = Regex.Replace(code, pattern, match =>
            {
                if (match.Value.StartsWith("/") || match.Value.StartsWith("#"))
                    return new string('\n', match.Value.Count(c => c == '\n'));
                return " STR ";
            }, RegexOptions.Multiline);

            normalized = Regex.Replace(normalized, @"\b\d+\b", " NUM ");
            normalized = Regex.Replace(normalized, @"\b(if|else|for|while|return|class|public|private|static|int|string|void|var)\b", " KEY ");
            normalized = Regex.Replace(normalized, @"[a-zA-Z_][a-zA-Z0-9_]*", " ID ");
            var words = normalized.Split(new[] { ' ', '\t', '\n', '\r', '{', '}', '(', ')', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            int line = 1;
            foreach (var word in words)
                tokens.Add(new TokenInfo { Value = word, Line = line++ });
            return tokens;
        }
    }
}