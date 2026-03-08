using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Services
{
    public class MetricAnalysisService : IMetricAnalysisService
    {
        private readonly IEnumerable<IMetricAlgorithm> _algorithms;

        public MetricAnalysisService(IEnumerable<IMetricAlgorithm> algorithms)
        {
            _algorithms = algorithms;
        }

        public AnalysisResponseDto Analyze(MetricAnalysisRequest request)
        {
            var allMatches = new List<DetailedMatch>();
            double totalScore = 0;
            int algoCount = 0;
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
                    totalScore += score;
                    algoCount++;
                }
            }

            double averageScore = algoCount > 0 ? totalScore / algoCount : 0;

            return new AnalysisResponseDto
            {
                Info = new AnalysisInfo
                {
                    OverallScore = (int)averageScore,
                    Mode = "Metrics-Based Analysis",
                    Date = DateTime.Now.ToString("dd.MM.yyyy"),
                },
                SubmissionTree = new List<NodeDto>
                {
                    new NodeDto
                    {
                        Name = fileA?.Filename ?? "unknown",
                        Type = "file",
                        Score = (int)averageScore,
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
