using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Services
{
    public class GraphAnalysisService : IGraphAnalysisService
    {
        private readonly IEnumerable<IGraphAlgorithm> _algorithms;

        public GraphAnalysisService(IEnumerable<IGraphAlgorithm> algorithms)
        {
            _algorithms = algorithms;
        }

        public AnalysisResponse Analyze(AnalysisRequest request)
        {
            var submissionNodes = new List<NodeDto>();
            double globalMaxScore = 0;
            var requestedMethods = request.AnalysisConfig?.Methods ?? new List<string>();

            var algoParams = request.AnalysisConfig?.Parameters?.ToDictionary(
                k => k.Key,
                v => (object)v.Value
            );

            foreach (var fileA in request.SubmissionA.Files)
            {
                var fileNode = new NodeDto
                {
                    Name = fileA.Filename,
                    Path = fileA.Filename,
                    Type = "file",
                    ReferenceScores = new Dictionary<string, int>(),
                    DetailedMatches = new Dictionary<string, List<DetailedMatch>>(),
                };

                double fileBestScore = 0;

                foreach (var fileB in request.SubmissionB.Files)
                {
                    var pairMatches = new List<DetailedMatch>();
                    double pairBestScore = 0;

                    foreach (var algo in _algorithms)
                    {
                        if (requestedMethods.Any() && !requestedMethods.Contains(algo.Key))
                            continue;

                        var matches = algo.Execute(
                            fileA.Content,
                            fileB.Content,
                            algoParams,
                            out double score
                        );

                        pairMatches.AddRange(matches);
                        pairBestScore = Math.Max(pairBestScore, score);
                    }

                    fileNode.DetailedMatches[fileB.Filename] = pairMatches;
                    fileNode.ReferenceScores[fileB.Filename] = (int)pairBestScore;
                    fileBestScore = Math.Max(fileBestScore, pairBestScore);
                }

                fileNode.Score = (int)fileBestScore;
                globalMaxScore = Math.Max(globalMaxScore, fileBestScore);
                submissionNodes.Add(fileNode);
            }

            return new AnalysisResponse
            {
                Info = new AnalysisInfo
                {
                    OverallScore = (int)globalMaxScore,
                    Mode = "Graph-Based (CFG/PDG) Analysis",
                    Date = DateTime.Now.ToString("dd.MM.yyyy"),
                },
                SubmissionTree = submissionNodes,
                ReferenceTree = request
                    .SubmissionB.Files.Select(f => new NodeDto
                    {
                        Name = f.Filename,
                        Type = "file",
                        Path = f.Filename,
                    })
                    .ToList(),
            };
        }
    }
}
