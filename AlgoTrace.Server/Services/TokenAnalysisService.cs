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
                    DetailedMatches = new Dictionary<string, List<DetailedMatch>>(),
                };

                var sourceTokens = Tokenize(fileA.Content);
                double fileBestScore = 0;

                foreach (var fileB in request.SubmissionB.Files)
                {
                    var referenceTokens = Tokenize(fileB.Content);
                    var pairMatches = new List<DetailedMatch>();
                    double pairMaxScore = 0;

                    foreach (var algo in _algorithms)
                    {
                        string algoKey = algo.Name.ToLower().Replace(" ", "_");

                        if (requestedMethods.Any() && !requestedMethods.Contains(algoKey))
                            continue;

                        var matches = algo.Execute(sourceTokens, referenceTokens, out double score);

                        pairMatches.AddRange(matches);
                        if (score > pairMaxScore)
                            pairMaxScore = score;
                    }

                    fileNode.DetailedMatches[fileB.Filename] = pairMatches;
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
                    Mode = "Lexical Token Analysis",
                    Date = DateTime.Now.ToString("dd.MM.yyyy"),
                },
                SubmissionTree = submissionNodes,
                ReferenceTree = request
                    .SubmissionB.Files.Select(f => new NodeDto
                    {
                        Name = f.Filename,
                        Path = f.Filename,
                        Type = "file",
                    })
                    .ToList(),
            };
        }

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

            foreach (var word in words)
            {
                tokens.Add(new TokenInfo { Value = word, Line = 0 });
            }

            return tokens;
        }
    }
}
