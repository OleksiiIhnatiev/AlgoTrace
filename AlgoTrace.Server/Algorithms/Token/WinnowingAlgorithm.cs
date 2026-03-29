using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO.Analysis;
using System.Collections.Generic;
using System.Linq;

namespace AlgoTrace.Server.Algorithms.Token
{
    public class WinnowingAlgorithm : ITokenAlgorithm
    {
        public string Key => "winnowing";
        public string Name => "Winnowing (Fingerprinting)";
        private const int K = 5;

        public List<DetailedMatch> Execute(
            List<TokenInfo> sourceTokens,
            List<TokenInfo> targetTokens,
            out double similarityScore
        )
        {
            if (
                sourceTokens == null
                || sourceTokens.Count < K
                || targetTokens == null
                || targetTokens.Count < K
            )
            {
                similarityScore = 0;
                return new List<DetailedMatch>();
            }

            var sHashes = GetFingerprints(sourceTokens);
            var tHashes = GetFingerprints(targetTokens);

            if (!sHashes.Any() || !tHashes.Any())
            {
                similarityScore = 0;
                return new List<DetailedMatch>();
            }

            var targetHashSet = tHashes.Select(h => h.Hash).ToHashSet();
            var commonHashes = sHashes
                .Select(h => h.Hash)
                .Where(hash => targetHashSet.Contains(hash))
                .ToHashSet();

            var rawMatches = new List<(int SourceIndex, int TargetIndex, List<TokenInfo> LeftTokens, List<TokenInfo> RightTokens)>();

            foreach (var hash in commonHashes)
            {
                var sourceOccurrences = sHashes.Where(h => h.Hash == hash).ToList();
                var targetOccurrences = tHashes.Where(h => h.Hash == hash).ToList();

                var usedTargetIndices = new HashSet<int>();

                foreach (var sMatch in sourceOccurrences)
                {
                    for (int i = 0; i < targetOccurrences.Count; i++)
                    {
                        if (usedTargetIndices.Contains(i))
                            continue;

                        var tMatch = targetOccurrences[i];
                        rawMatches.Add((sMatch.TokenIndex, tMatch.TokenIndex, sMatch.Tokens, tMatch.Tokens));

                        usedTargetIndices.Add(i);
                        break;
                    }
                }
            }

            var groups = rawMatches.GroupBy(m => m.SourceIndex - m.TargetIndex);
            var mergedBlocks = new List<Block>();

            foreach (var group in groups)
            {
                var sorted = group.OrderBy(m => m.SourceIndex).ToList();

                var currentSourceStart = sorted[0].SourceIndex;
                var currentSourceEnd = currentSourceStart + K;
                var currentTargetStart = sorted[0].TargetIndex;
                var currentTargetEnd = currentTargetStart + K;
                var currentLeftTokens = new List<TokenInfo>(sorted[0].LeftTokens);
                var currentRightTokens = new List<TokenInfo>(sorted[0].RightTokens);

                for (int i = 1; i < sorted.Count; i++)
                {
                    var next = sorted[i];

                    if (next.SourceIndex <= currentSourceEnd)
                    {
                        int overlap = currentSourceEnd - next.SourceIndex;
                        int newTokensCount = K - overlap;

                        if (newTokensCount > 0)
                        {
                            currentLeftTokens.AddRange(next.LeftTokens.Skip(overlap));
                            currentRightTokens.AddRange(next.RightTokens.Skip(overlap));
                            currentSourceEnd = next.SourceIndex + K;
                            currentTargetEnd = next.TargetIndex + K;
                        }
                    }
                    else
                    {
                        mergedBlocks.Add(new Block(currentSourceStart, currentSourceEnd, currentTargetStart, currentTargetEnd, currentLeftTokens, currentRightTokens));

                        currentSourceStart = next.SourceIndex;
                        currentSourceEnd = currentSourceStart + K;
                        currentTargetStart = next.TargetIndex;
                        currentTargetEnd = currentTargetStart + K;
                        currentLeftTokens = new List<TokenInfo>(next.LeftTokens);
                        currentRightTokens = new List<TokenInfo>(next.RightTokens);
                    }
                }

                mergedBlocks.Add(new Block(currentSourceStart, currentSourceEnd, currentTargetStart, currentTargetEnd, currentLeftTokens, currentRightTokens));
            }

            mergedBlocks = mergedBlocks.OrderByDescending(b => b.SourceEnd - b.SourceStart).ToList();
            var finalBlocks = new List<Block>();
            var usedSourceIntervals = new List<(int start, int end)>();
            var usedTargetIntervals = new List<(int start, int end)>();

            foreach (var block in mergedBlocks)
            {
                bool overlaps = false;

                foreach (var interval in usedSourceIntervals)
                {
                    if (block.SourceStart < interval.end && block.SourceEnd > interval.start)
                    {
                        overlaps = true;
                        break;
                    }
                }
                if (overlaps) continue;

                foreach (var interval in usedTargetIntervals)
                {
                    if (block.TargetStart < interval.end && block.TargetEnd > interval.start)
                    {
                        overlaps = true;
                        break;
                    }
                }
                if (overlaps) continue;

                finalBlocks.Add(block);
                usedSourceIntervals.Add((block.SourceStart, block.SourceEnd));
                usedTargetIntervals.Add((block.TargetStart, block.TargetEnd));
            }

            int matchedSourceTokensCount = finalBlocks.Sum(b => b.LeftTokens.Count);
            similarityScore = sourceTokens.Count > 0 ? ((double)matchedSourceTokensCount / sourceTokens.Count) * 100 : 0;

            var matches = new List<DetailedMatch>();
            int matchId = 5000;
            string severity = similarityScore > 70 ? "high" : "med";

            foreach (var block in finalBlocks.OrderBy(b => b.SourceStart))
            {
                matches.Add(new DetailedMatch
                {
                    Id = matchId++,
                    Type = "Token Sequence Match",
                    Severity = severity,
                    LeftLines = new List<int>
                    {
                        block.LeftTokens.First().Line,
                        block.LeftTokens.Last().Line
                    },
                    RightLines = new List<int>
                    {
                        block.RightTokens.First().Line,
                        block.RightTokens.Last().Line
                    },
                    LeftTokens = block.LeftTokens,
                    RightTokens = block.RightTokens
                });
            }

            return matches;
        }

        private record Block(int SourceStart, int SourceEnd, int TargetStart, int TargetEnd, List<TokenInfo> LeftTokens, List<TokenInfo> RightTokens);

        private record Fingerprint(int Hash, int TokenIndex, List<TokenInfo> Tokens);

        private List<Fingerprint> GetFingerprints(List<TokenInfo> tokens)
        {
            var result = new List<Fingerprint>();

            for (int i = 0; i <= tokens.Count - K; i++)
            {
                var window = tokens.Skip(i).Take(K).ToList();
                var gram = string.Join("", window.Select(t => t.Value));
                result.Add(new Fingerprint(gram.GetHashCode(), i, window));
            }
            return result;
        }
    }
}