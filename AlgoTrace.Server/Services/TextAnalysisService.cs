using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Services
{
    public class TextAnalysisService : ITextAnalysisService
    {
        private readonly IEnumerable<ITextAlgorithm> _algorithms;

        public TextAnalysisService(IEnumerable<ITextAlgorithm> algorithms)
        {
            _algorithms = algorithms;
        }

        public AnalysisResponse Analyze(AnalysisRequest request)
        {
            var submissionNodes = new List<NodeDto>();
            double globalMaxScore = 0;
            var requestedMethods = request.AnalysisConfig?.Methods ?? new List<string>();

            foreach (var fileA in request.SubmissionA.Files)
            {
                var fileNode = new NodeDto
                {
                    Name = fileA.Filename,
                    Path = fileA.Filename,
                    Type = "file",
                    ReferenceScores = new Dictionary<string, int>(),
                    DetailedMatches = new Dictionary<string, List<DetailedMatch>>()
                };

                double fileBestScore = 0;

                foreach (var fileB in request.SubmissionB.Files)
                {
                    var pairMatches = new List<DetailedMatch>();
                    double pairMaxScore = 0;

                    foreach (var algo in _algorithms)
                    {
                        if (requestedMethods.Any() && !requestedMethods.Contains(algo.Name.ToLower().Replace(" ", "_")))
                            continue;

                        var matches = algo.Execute(fileA.Content, fileB.Content, out double score);

                        pairMatches.AddRange(matches);
                        if (score > pairMaxScore)
                            pairMaxScore = score;
                    }

                    var optimizedMatches = CollapseMatches(pairMatches);

                    fileNode.DetailedMatches[fileB.Filename] = optimizedMatches;
                    fileNode.ReferenceScores[fileB.Filename] = (int)pairMaxScore;

                    if (pairMaxScore > fileBestScore)
                        fileBestScore = pairMaxScore;
                }

                fileNode.Score = (int)fileBestScore;
                if (fileBestScore > globalMaxScore)
                    globalMaxScore = fileBestScore;

                submissionNodes.Add(fileNode);
            }

            return new AnalysisResponse
            {
                Info = new AnalysisInfo
                {
                    OverallScore = (int)globalMaxScore,
                    Mode = "Multi-File Textual Analysis",
                    Date = DateTime.Now.ToString("dd.MM.yyyy"),
                },
                SubmissionTree = submissionNodes,
                ReferenceTree = request.SubmissionB.Files.Select(f => new NodeDto
                {
                    Name = f.Filename,
                    Path = f.Filename,
                    Type = "file"
                }).ToList()
            };
        }

        private List<DetailedMatch> CollapseMatches(List<DetailedMatch> matches)
        {
            if (matches == null || !matches.Any())
                return new List<DetailedMatch>();

            var sorted = matches.OrderBy(m => m.LeftLines[0]).ToList();
            var result = new List<DetailedMatch>();

            var current = sorted[0];

            for (int i = 1; i < sorted.Count; i++)
            {
                var next = sorted[i];

                if (next.LeftLines[0] <= current.LeftLines[1] + 1)
                {
                    current.LeftLines[1] = Math.Max(current.LeftLines[1], next.LeftLines[1]);
                    current.RightLines[1] = Math.Max(current.RightLines[1], next.RightLines[1]);

                    if (next.Severity == "high")
                        current.Severity = "high";
                }
                else
                {
                    result.Add(current);
                    current = next;
                }
            }
            result.Add(current);

            for (int i = 0; i < result.Count; i++)
                result[i].Id = i + 1;

            return result;
        }
    }
}