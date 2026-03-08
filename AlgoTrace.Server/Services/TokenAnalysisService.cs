using System.Text.RegularExpressions;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Services
{
    public class TokenAnalysisService : ITokenAnalysisService
    {
        private readonly IEnumerable<ITokenAlgorithm> _algorithms;

        public TokenAnalysisService(IEnumerable<ITokenAlgorithm> algorithms)
        {
            _algorithms = algorithms;
        }

        public AnalysisResponseDto Analyze(
            string source,
            string reference,
            string sourceName,
            string refName
        )
        {
            var sourceTokens = Tokenize(source);
            var referenceTokens = Tokenize(reference);

            var allMatches = new List<DetailedMatch>();
            double maxScore = 0;

            foreach (var algo in _algorithms)
            {
                var matches = algo.Execute(sourceTokens, referenceTokens, out double score);
                allMatches.AddRange(matches);
                if (score > maxScore)
                    maxScore = score;
            }

            return new AnalysisResponseDto
            {
                Info = new AnalysisInfo
                {
                    OverallScore = (int)maxScore,
                    Mode = "Lexical Token Analysis",
                    Date = DateTime.Now.ToString("dd.MM.yyyy"),
                },
                SubmissionTree = new List<NodeDto>
                {
                    new NodeDto
                    {
                        Name = sourceName,
                        Path = sourceName,
                        Type = "file",
                        Score = (int)maxScore,
                        ReferenceScores = new Dictionary<string, int>
                        {
                            { refName, (int)maxScore },
                        },
                        DetailedMatches = new Dictionary<string, List<DetailedMatch>>
                        {
                            { refName, allMatches },
                        },
                    },
                },
                ReferenceTree = new List<NodeDto>
                {
                    new NodeDto
                    {
                        Name = refName,
                        Path = refName,
                        Type = "file",
                    },
                },
            };
        }

        public List<TokenInfo> Tokenize(string code)
        {
            var tokens = new List<TokenInfo>();
            var lines = code.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var words = lines[i]
                    .Split(new[] { ' ', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    tokens.Add(new TokenInfo { Value = word, Line = i + 1 });
                }
            }
            return tokens;
        }
    }
}
