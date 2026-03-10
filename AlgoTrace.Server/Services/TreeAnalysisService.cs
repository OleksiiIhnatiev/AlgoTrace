using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Services
{
    public class TreeAnalysisService : ITreeAnalysisService
    {
        private readonly IEnumerable<ICodeParser> _parsers;
        private readonly IEnumerable<ITreeAlgorithm> _algorithms;

        public TreeAnalysisService(IEnumerable<ICodeParser> parsers, IEnumerable<ITreeAlgorithm> algorithms)
        {
            _parsers = parsers;
            _algorithms = algorithms;
        }

        public AnalysisResponse Analyze(AnalysisRequest request)
        {
            var parser = _parsers.FirstOrDefault(p => p.Language.Equals(request.Language, StringComparison.OrdinalIgnoreCase))
                         ?? throw new Exception($"Language {request.Language} not supported");

            var submissionNodes = new List<NodeDto>();
            double globalMaxScore = 0;

            foreach (var fileA in request.SubmissionA.Files)
            {
                var treeA = parser.Parse(fileA.Content);
                var node = new NodeDto { Name = fileA.Filename, Path = fileA.Filename, Type = "file" };
                double fileBestScore = 0;

                foreach (var fileB in request.SubmissionB.Files)
                {
                    var treeB = parser.Parse(fileB.Content);
                    var pairMatches = new List<DetailedMatch>();
                    double pairBestScore = 0;

                    foreach (var algo in _algorithms)
                    {
                        if (request.AnalysisConfig.Methods.Contains(algo.Key))
                        {
                            double score = algo.Calculate(treeA, treeB, out var m);
                            pairMatches.AddRange(m);
                            pairBestScore = Math.Max(pairBestScore, score);
                        }
                    }

                    node.ReferenceScores[fileB.Filename] = (int)pairBestScore;
                    node.DetailedMatches[fileB.Filename] = pairMatches;
                    fileBestScore = Math.Max(fileBestScore, pairBestScore);
                }

                node.Score = (int)fileBestScore;
                globalMaxScore = Math.Max(globalMaxScore, fileBestScore);
                submissionNodes.Add(node);
            }

            return new AnalysisResponse
            {
                Info = new AnalysisInfo { OverallScore = (int)globalMaxScore, Mode = "AST Tree-based", Date = DateTime.Now.ToString("dd.MM.yyyy") },
                SubmissionTree = submissionNodes
            };
        }
    }
}
