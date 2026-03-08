using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;
using AlgoTrace.Server.Utils;

namespace AlgoTrace.Server.Services
{
    public class TextAnalysisService : ITextAnalysisService
    {
        private readonly IEnumerable<ITextAlgorithm> _algorithms;

        public TextAnalysisService(IEnumerable<ITextAlgorithm> algorithms)
        {
            _algorithms = algorithms;
        }

        public AnalysisResponseDto Analyze(
            string source,
            string reference,
            string sourcePath,
            string refPath
        )
        {
            var allMatches = new List<DetailedMatch>();
            double maxScore = 0;

            foreach (var algo in _algorithms)
            {
                var matches = algo.Execute(source, reference, out double score);
                allMatches.AddRange(matches);
                if (score > maxScore)
                    maxScore = score;
            }

            var optimizedMatches = CollapseMatches(allMatches);

            var sourceFileName = Path.GetFileName(sourcePath);
            var refFileName = Path.GetFileName(refPath);

            return new AnalysisResponseDto
            {
                Info = new AnalysisInfo
                {
                    OverallScore = (int)maxScore,
                    Mode = "Multi-Layer Textual Analysis",
                    Date = DateTime.Now.ToString("dd.MM.yyyy"),
                },
                SubmissionTree = new List<NodeDto>
                {
                    new NodeDto
                    {
                        Name = sourceFileName,
                        Path = sourcePath,
                        Type = "file",
                        Score = (int)maxScore,
                        ReferenceScores = new Dictionary<string, int>
                        {
                            { refFileName, (int)maxScore },
                        },
                        DetailedMatches = new Dictionary<string, List<DetailedMatch>>
                        {
                            { refFileName, optimizedMatches },
                        },
                    },
                },
                ReferenceTree = new List<NodeDto>
                {
                    new NodeDto
                    {
                        Name = refFileName,
                        Path = refPath,
                        Type = "file",
                    },
                },
            };
        }

        private List<DetailedMatch> CollapseMatches(List<DetailedMatch> matches)
        {
            if (!matches.Any())
                return matches;

            var sorted = matches.OrderBy(m => m.LeftLines[0]).ToList();
            var result = new List<DetailedMatch>();

            if (sorted.Count == 0)
                return result;

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
