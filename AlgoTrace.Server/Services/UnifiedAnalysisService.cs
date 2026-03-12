using System.Text.Json;
using System.Text.RegularExpressions;
using AlgoTrace.Server.Algorithms.Metric;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;
using AlgoTrace.Server.ParserFactory;

namespace AlgoTrace.Server.Services
{
    public class UnifiedAnalysisService : IUnifiedAnalysisService
    {
        private readonly IEnumerable<ITextAlgorithm> _textAlgos;
        private readonly IEnumerable<ITokenAlgorithm> _tokenAlgos;
        private readonly IEnumerable<ITreeAlgorithm> _treeAlgos;
        private readonly IEnumerable<IGraphAlgorithm> _graphAlgos;
        private readonly IEnumerable<IMetricAlgorithm> _metricAlgos;
        private readonly IEnumerable<ICodeParser> _parsers;

        public UnifiedAnalysisService(
            IEnumerable<ITextAlgorithm> textAlgos,
            IEnumerable<ITokenAlgorithm> tokenAlgos,
            IEnumerable<ITreeAlgorithm> treeAlgos,
            IEnumerable<IGraphAlgorithm> graphAlgos,
            IEnumerable<IMetricAlgorithm> metricAlgos,
            IEnumerable<ICodeParser> parsers
        )
        {
            _textAlgos = textAlgos;
            _tokenAlgos = tokenAlgos;
            _treeAlgos = treeAlgos;
            _graphAlgos = graphAlgos;
            _metricAlgos = metricAlgos;
            _parsers = parsers;
        }

        public UnifiedAnalysisResponse Analyze(UnifiedAnalysisRequest request)
        {
            var response = new UnifiedAnalysisResponse
            {
                AnalysisId = $"req_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Status = "completed",
                CategoriesResults = new List<CategoryResult>(),
            };

            // Pre-process contents (Simplification: Taking first file from each submission for comparison)
            var fileA = request.SubmissionA.Files.FirstOrDefault();
            var fileB = request.SubmissionB.Files.FirstOrDefault();

            if (fileA == null || fileB == null)
                return response;

            string contentA = fileA.Content;
            string contentB = fileB.Content;
            var parameters = request.AnalysisConfig.Parameters;
            var categories = request.AnalysisConfig.ExecuteCategories;

            // 1. Text Based
            if (categories.TextBased != null && categories.TextBased.Any())
            {
                var result = ProcessText(
                    contentA,
                    contentB,
                    categories.TextBased,
                    fileA.Filename,
                    fileB.Filename
                );
                response.CategoriesResults.Add(result);
            }

            // 2. Token Based
            if (categories.TokenBased != null && categories.TokenBased.Any())
            {
                var result = ProcessToken(contentA, contentB, categories.TokenBased);
                response.CategoriesResults.Add(result);
            }

            // 3. Tree Based
            if (categories.TreeBased != null && categories.TreeBased.Any())
            {
                var result = ProcessTree(
                    contentA,
                    contentB,
                    request.Language,
                    categories.TreeBased
                );
                response.CategoriesResults.Add(result);
            }

            // 4. Graph Based
            if (categories.GraphBased != null && categories.GraphBased.Any())
            {
                var result = ProcessGraph(contentA, contentB, categories.GraphBased, parameters);
                response.CategoriesResults.Add(result);
            }

            // 5. Metrics Based
            if (categories.MetricsBased != null && categories.MetricsBased.Any())
            {
                var result = ProcessMetrics(
                    contentA,
                    contentB,
                    categories.MetricsBased,
                    parameters
                );
                response.CategoriesResults.Add(result);
            }

            // Calculate Global Score (Average of categories)
            if (response.CategoriesResults.Any())
            {
                response.GlobalSimilarityScore = Math.Round(
                    response.CategoriesResults.Average(c => c.CategorySimilarityScore),
                    2
                );
            }

            return response;
        }

        private CategoryResult ProcessText(
            string a,
            string b,
            List<string> methods,
            string nameA,
            string nameB
        )
        {
            var categoryResult = new CategoryResult { CategoryName = "text_based" };
            double totalScore = 0;
            int count = 0;

            foreach (var algo in _textAlgos)
            {
                // Map config names (e.g. "exact_substring") to algo names/keys
                string algoKey = algo.Key;
                if (!methods.Contains(algoKey))
                    continue;

                var matches = algo.Execute(a, b, out double score);
                totalScore += score / 100.0; // Normalize to 0-1
                count++;

                var evidence = new TextEvidence();
                foreach (var m in matches)
                {
                    evidence.MatchedBlocks.Add(
                        new
                        {
                            file_a = nameA,
                            file_b = nameB,
                            start_line_a = m.LeftLines.FirstOrDefault(),
                            end_line_a = m.LeftLines.LastOrDefault(),
                            start_line_b = m.RightLines.FirstOrDefault(),
                            end_line_b = m.RightLines.LastOrDefault(),
                            content = "Snippet hidden for brevity", // Or extract from source using lines
                        }
                    );
                }

                categoryResult.Algorithms.Add(
                    new AlgorithmResult
                    {
                        Method = algoKey,
                        SimilarityScore = Math.Round(score / 100.0, 2),
                        EvidenceType = "text_highlight",
                        Evidence = evidence,
                    }
                );
            }

            categoryResult.CategorySimilarityScore =
                count > 0 ? Math.Round(totalScore / count, 2) : 0;
            return categoryResult;
        }

        private CategoryResult ProcessToken(string a, string b, List<string> methods)
        {
            var categoryResult = new CategoryResult { CategoryName = "token_based" };

            // Simple tokenizer logic reused from TokenAnalysisService
            var tokensA = Tokenize(a);
            var tokensB = Tokenize(b);

            double totalScore = 0;
            int count = 0;

            foreach (var algo in _tokenAlgos)
            {
                if (!methods.Contains(algo.Key))
                    continue;

                var matches = algo.Execute(tokensA, tokensB, out double score);
                totalScore += score / 100.0;
                count++;

                // Mocking structure based on matches since existing algos return generic match
                var evidence = new TokenEvidence();
                if (matches.Any())
                {
                    evidence.MatchedHashes.Add(
                        new
                        {
                            hash_value = "generated_hash",
                            token_sequence = "KEYWORD(while) IDENTIFIER ...",
                            occurrences = new[]
                            {
                                new
                                {
                                    submission = "a",
                                    token_start_index = matches[0].LeftLines[0],
                                    token_end_index = matches[0].LeftLines[1],
                                },
                            },
                        }
                    );
                }

                categoryResult.Algorithms.Add(
                    new AlgorithmResult
                    {
                        Method = algo.Key,
                        SimilarityScore = Math.Round(score / 100.0, 2),
                        EvidenceType = "token_sequence",
                        Evidence = evidence,
                    }
                );
            }
            categoryResult.CategorySimilarityScore =
                count > 0 ? Math.Round(totalScore / count, 2) : 0;
            return categoryResult;
        }

        private CategoryResult ProcessTree(string a, string b, string lang, List<string> methods)
        {
            var categoryResult = new CategoryResult { CategoryName = "tree_based" };
            var parser = _parsers.FirstOrDefault(p =>
                p.Language.Equals(lang, StringComparison.OrdinalIgnoreCase)
            );

            if (parser == null)
                return categoryResult;

            var treeA = parser.Parse(a);
            var treeB = parser.Parse(b);

            double totalScore = 0;
            int count = 0;

            foreach (var algo in _treeAlgos)
            {
                if (!methods.Contains(algo.Key))
                    continue;

                double score = algo.Calculate(treeA, treeB, out object evidence);
                totalScore += score / 100.0;
                count++;

                categoryResult.Algorithms.Add(
                    new AlgorithmResult
                    {
                        Method = algo.Key,
                        SimilarityScore = Math.Round(score / 100.0, 2),
                        EvidenceType = "ast_tree_mapping",
                        Evidence = evidence,
                    }
                );
            }
            categoryResult.CategorySimilarityScore =
                count > 0 ? Math.Round(totalScore / count, 2) : 0;
            return categoryResult;
        }

        private CategoryResult ProcessGraph(
            string a,
            string b,
            List<string> methods,
            Dictionary<string, object> parameters
        )
        {
            var categoryResult = new CategoryResult { CategoryName = "graph_based" };
            double totalScore = 0;
            int count = 0;

            foreach (var algo in _graphAlgos)
            {
                if (!methods.Contains(algo.Key))
                    continue;

                var matches = algo.Execute(a, b, parameters, out double score);
                totalScore += score / 100.0;
                count++;

                categoryResult.Algorithms.Add(
                    new AlgorithmResult
                    {
                        Method = algo.Key,
                        SimilarityScore = Math.Round(score / 100.0, 2),
                        EvidenceType = "graph_mapping",
                        Evidence = new
                        {
                            nodes = new[] { new { id = "n1", type = "mock_node" } },
                            edges = new object[] { },
                        },
                    }
                );
            }
            categoryResult.CategorySimilarityScore =
                count > 0 ? Math.Round(totalScore / count, 2) : 0;
            return categoryResult;
        }

        private CategoryResult ProcessMetrics(
            string a,
            string b,
            List<string> methods,
            Dictionary<string, object> parameters
        )
        {
            var categoryResult = new CategoryResult { CategoryName = "metrics_based" };
            double totalScore = 0;
            int count = 0;

            foreach (var algo in _metricAlgos)
            {
                if (!methods.Contains(algo.Key))
                    continue;

                var matches = algo.Execute(a, b, parameters, out double score);
                totalScore += score / 100.0;
                count++;

                var evidence = new MetricEvidence
                {
                    Conclusion = $"Metrics similarity: {score:F1}%",
                };

                // Re-calculate local metrics to populate the evidence fields specifically
                if (algo.Key == "halstead")
                {
                    var hA = MetricUtils.CalculateHalsteadMetrics(a);
                    var hB = MetricUtils.CalculateHalsteadMetrics(b);
                    evidence.MetricsA = new Dictionary<string, double>
                    {
                        { "volume", hA.Volume },
                        { "difficulty", hA.Difficulty },
                    };
                    evidence.MetricsB = new Dictionary<string, double>
                    {
                        { "volume", hB.Volume },
                        { "difficulty", hB.Difficulty },
                    };
                }

                categoryResult.Algorithms.Add(
                    new AlgorithmResult
                    {
                        Method = algo.Key,
                        SimilarityScore = Math.Round(score / 100.0, 2),
                        EvidenceType = "metric_comparison",
                        Evidence = evidence,
                    }
                );
            }
            categoryResult.CategorySimilarityScore =
                count > 0 ? Math.Round(totalScore / count, 2) : 0;
            return categoryResult;
        }

        // Helper reused from TokenAnalysisService (Duplicated to avoid tight coupling for this refactor)
        private List<TokenInfo> Tokenize(string code)
        {
            var tokens = new List<TokenInfo>();
            var noComments = Regex.Replace(code, @"//.*|/\*[\s\S]*?\*/", " ");
            string normalized = noComments;
            normalized = Regex.Replace(normalized, @"""[^""""]*""", " STR ");
            normalized = Regex.Replace(normalized, @"\b\d+\b", " NUM ");
            normalized = Regex.Replace(
                normalized,
                @"\b(if|else|for|while|return|class|public|private|static|int|string|void|var)\b",
                " KEY "
            );
            normalized = Regex.Replace(normalized, @"[a-zA-Z_][a-zA-Z0-9_]*", " ID ");
            var words = normalized.Split(
                new[] { ' ', '\t', '\n', '\r', '{', '}', '(', ')', ';', ',' },
                StringSplitOptions.RemoveEmptyEntries
            );
            int line = 1;
            foreach (var word in words)
                tokens.Add(new TokenInfo { Value = word, Line = line++ });
            return tokens;
        }
    }
}
