using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Algorithms
{
    public class JaccardTokenAlgorithm : ITokenAlgorithm
    {
        public string Name => "Jaccard Token Similarity";

        public List<DetailedMatch> Execute(
            List<TokenInfo> sourceTokens,
            List<TokenInfo> targetTokens,
            out double similarityScore
        )
        {
            var set1 = sourceTokens.Select(t => t.Value).ToHashSet();
            var set2 = targetTokens.Select(t => t.Value).ToHashSet();

            double intersection = set1.Intersect(set2).Count();
            double union = set1.Union(set2).Count();
            similarityScore = (intersection / union) * 100;

            return new List<DetailedMatch>
            {
                new DetailedMatch
                {
                    Id = 202,
                    Type = "Token Vocabulary Similarity",
                    Severity = similarityScore > 80 ? "high" : "low",
                    LeftLines = new List<int>
                    {
                        sourceTokens.First().Line,
                        sourceTokens.Last().Line,
                    },
                    RightLines = new List<int>
                    {
                        targetTokens.First().Line,
                        targetTokens.Last().Line,
                    },
                },
            };
        }
    }
}
