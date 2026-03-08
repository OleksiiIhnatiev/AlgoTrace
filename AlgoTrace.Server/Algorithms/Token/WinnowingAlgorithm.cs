using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Algorithms
{
    public class WinnowingAlgorithm : ITokenAlgorithm
    {
        public string Name => "Winnowing (Fingerprinting)";
        private const int K = 5;

        public List<DetailedMatch> Execute(
            List<TokenInfo> sourceTokens,
            List<TokenInfo> targetTokens,
            out double similarityScore
        )
        {
            var sHashes = GetFingerprints(sourceTokens);
            var tHashes = GetFingerprints(targetTokens);

            if (!sHashes.Any() || !tHashes.Any())
            {
                similarityScore = 0;
                return new List<DetailedMatch>();
            }

            var commonHashes = sHashes
                .Select(h => h.Hash)
                .Intersect(tHashes.Select(h => h.Hash))
                .ToHashSet();
            similarityScore =
                (double)commonHashes.Count / Math.Max(sHashes.Count, tHashes.Count) * 100;

            var matches = new List<DetailedMatch>();
            if (commonHashes.Any())
            {
                var matchedSource = sHashes.Where(h => commonHashes.Contains(h.Hash)).ToList();
                var matchedTarget = tHashes.Where(h => commonHashes.Contains(h.Hash)).ToList();

                matches.Add(
                    new DetailedMatch
                    {
                        Id = 201,
                        Type = "Token Sequence Match",
                        Severity = similarityScore > 70 ? "high" : "med",
                        LeftLines = new List<int>
                        {
                            matchedSource.First().StartLine,
                            matchedSource.Last().EndLine,
                        },
                        RightLines = new List<int>
                        {
                            matchedTarget.First().StartLine,
                            matchedTarget.Last().EndLine,
                        },
                    }
                );
            }
            return matches;
        }

        private record Fingerprint(int Hash, int StartLine, int EndLine);

        private List<Fingerprint> GetFingerprints(List<TokenInfo> tokens)
        {
            var result = new List<Fingerprint>();
            for (int i = 0; i <= tokens.Count - K; i++)
            {
                var window = tokens.Skip(i).Take(K).ToList();
                var gram = string.Join("", window.Select(t => t.Value));
                result.Add(
                    new Fingerprint(gram.GetHashCode(), window.First().Line, window.Last().Line)
                );
            }
            return result;
        }
    }
}
