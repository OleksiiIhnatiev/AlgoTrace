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

        public AnalysisResponseDto Analyze(GraphAnalysisRequest request)
        {
            var allMatches = new List<DetailedMatch>();
            double maxScore = 0;
            var requestedMethods = request.AnalysisConfig?.Methods ?? new List<string>();

            var fileA = request.SubmissionA.Files.FirstOrDefault();
            var fileB = request.SubmissionB.Files.FirstOrDefault();

            if (fileA != null && fileB != null)
            {
                foreach (var algo in _algorithms)
                {
                    if (requestedMethods.Any() && !requestedMethods.Contains(algo.Key))
                        continue;

                    var matches = algo.Execute(
                        fileA.Content,
                        fileB.Content,
                        request.AnalysisConfig?.Parameters,
                        out double score
                    );

                    allMatches.AddRange(matches);
                    if (score > maxScore)
                        maxScore = score;
                }
            }

            return new AnalysisResponseDto
            {
                Info = new AnalysisInfo
                {
                    OverallScore = (int)maxScore,
                    Mode = "Graph-Based Analysis",
                    Date = DateTime.Now.ToString("dd.MM.yyyy"),
                },
                SubmissionTree = new List<NodeDto>
                {
                    new NodeDto
                    {
                        Name = fileA?.Filename ?? "unknown",
                        Type = "file",
                        Score = (int)maxScore,
                        DetailedMatches = new Dictionary<string, List<DetailedMatch>>
                        {
                            { fileB?.Filename ?? "unknown", allMatches },
                        },
                    },
                },
                ReferenceTree = new List<NodeDto>(),
            };
        }
    }
}
