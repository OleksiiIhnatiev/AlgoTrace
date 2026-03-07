using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;
using AlgoTrace.Server.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrace.Server.Algorithms
{
    public class LevenshteinAlgorithm : ITextAlgorithm
    {
        public string Name => "Levenshtein Line Comparison";

        public List<DetailedMatch> Execute(string source, string target, out double similarityScore)
        {
            var sLines = SourceNormalizer.GetLines(source);
            var tLines = SourceNormalizer.GetLines(target);
            var matches = new List<DetailedMatch>();
            int matchCount = 0;

            for (int i = 0; i < sLines.Length; i++)
            {
                string sNorm = SourceNormalizer.NormalizeLine(sLines[i]);
                if (sNorm.Length < 10) continue;

                for (int j = 0; j < tLines.Length; j++)
                {
                    string tNorm = SourceNormalizer.NormalizeLine(tLines[j]);
                    int dist = ComputeDistance(sNorm, tNorm);
                    double ratio = 1.0 - (double)dist / Math.Max(sNorm.Length, tNorm.Length);

                    if (ratio > 0.85)
                    {
                        matches.Add(new DetailedMatch
                        {
                            Id = i + 2000,
                            Type = "Fuzzy Line Match",
                            LeftLines = new List<int> { i + 1, i + 1 },
                            RightLines = new List<int> { j + 1, j + 1 },
                            Severity = "med"
                        });
                        matchCount++;
                        break;
                    }
                }
            }
            similarityScore = (double)matchCount / Math.Max(sLines.Length, tLines.Length) * 100;
            return matches;
        }

        private int ComputeDistance(string s, string t)
        {
            int n = s.Length, m = t.Length;
            int[,] d = new int[n + 1, m + 1];
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;
            for (int i = 1; i <= n; i++)
                for (int j = 1; j <= m; j++)
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + (s[i - 1] == t[j - 1] ? 0 : 1));
            return d[n, m];
        }
    }
}