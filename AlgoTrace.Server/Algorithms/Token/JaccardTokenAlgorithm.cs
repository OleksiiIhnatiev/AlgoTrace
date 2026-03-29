using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO.Analysis;
using System.Collections.Generic;
using System.Linq;

namespace AlgoTrace.Server.Algorithms.Token
{
    public class JaccardTokenAlgorithm : ITokenAlgorithm
    {
        public string Key => "jaccard_token";
        public string Name => "Jaccard Token Similarity";

        public List<DetailedMatch> Execute(
            List<TokenInfo> sourceTokens,
            List<TokenInfo> targetTokens,
            out double similarityScore
        )
        {
            if (
                sourceTokens == null
                || !sourceTokens.Any()
                || targetTokens == null
                || !targetTokens.Any()
            )
            {
                similarityScore = 0;
                return new List<DetailedMatch>();
            }

            var set1 = sourceTokens.Select(t => t.Value).ToHashSet();
            var set2 = targetTokens.Select(t => t.Value).ToHashSet();

            double intersection = set1.Intersect(set2).Count();
            double union = set1.Union(set2).Count();

            similarityScore = union > 0 ? (intersection / union) * 100 : 0;

            var matches = new List<DetailedMatch>();
            var commonTokens = set1.Intersect(set2).ToList();

            if (commonTokens.Any())
            {
                string severity = similarityScore > 80 ? "high" : "low";
                int matchId = 6000;

                var sourceDict = sourceTokens.GroupBy(t => t.Value).ToDictionary(g => g.Key, g => g.ToList());
                var targetDict = targetTokens.GroupBy(t => t.Value).ToDictionary(g => g.Key, g => g.ToList());

                foreach (var tokenValue in commonTokens)
                {
                    var sTokens = sourceDict[tokenValue];
                    var tTokens = targetDict[tokenValue];

                    matches.Add(new DetailedMatch
                    {
                        Id = matchId++,
                        Type = "Token Vocabulary Similarity",
                        Severity = severity,
                        LeftLines = new List<int>
                            {
                                sTokens.First().Line,
                                sTokens.Last().Line
                            },
                        RightLines = new List<int>
                            {
                                tTokens.First().Line,
                                tTokens.Last().Line
                            },
                        LeftTokens = sTokens,
                        RightTokens = tTokens
                    });
                }
            }

            return matches;
        }
    }
}