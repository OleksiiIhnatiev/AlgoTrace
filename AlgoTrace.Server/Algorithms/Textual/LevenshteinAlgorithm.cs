using System;
using System.Collections.Generic;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO.Analysis;
using AlgoTrace.Server.Utils;

namespace AlgoTrace.Server.Algorithms.Textual
{
    public class LevenshteinAlgorithm : ITextAlgorithm
    {
        public string Key => "levenshtein";
        public string Name => "Levenshtein Line Comparison";

        private const int MaxLineLength = 1000;

        public List<DetailedMatch> Execute(string source, string target, out double similarityScore)
        {
            var sLines = SourceNormalizer.GetLines(source);
            var tLines = SourceNormalizer.GetLines(target);
            var matches = new List<DetailedMatch>();
            int matchCount = 0;

            if (sLines.Length == 0 && tLines.Length == 0)
            {
                similarityScore = 100.0;
                return matches;
            }

            if (sLines.Length > 2500 || tLines.Length > 2500)
            {
                similarityScore = 0.0;
                return matches;
            }

            var sNorms = new string[sLines.Length];
            for (int i = 0; i < sLines.Length; i++)
                sNorms[i] = SourceNormalizer.NormalizeLine(sLines[i]);

            var tNorms = new string[tLines.Length];
            for (int j = 0; j < tLines.Length; j++)
                tNorms[j] = SourceNormalizer.NormalizeLine(tLines[j]);

            int validSourceLines = 0;

            for (int i = 0; i < sNorms.Length; i++)
            {
                string sNorm = sNorms[i];
                if (sNorm.Length < 5 || sNorm.Length > MaxLineLength)
                    continue;

                validSourceLines++;

                for (int j = 0; j < tNorms.Length; j++)
                {
                    string tNorm = tNorms[j];
                    if (tNorm.Length < 5 || tNorm.Length > MaxLineLength)
                        continue;

                    int maxLen = Math.Max(sNorm.Length, tNorm.Length);
                    if (Math.Abs(sNorm.Length - tNorm.Length) > maxLen * 0.15)
                        continue;

                    int dist = ComputeDistance(sNorm, tNorm);
                    double ratio = 1.0 - (double)dist / maxLen;

                    if (ratio > 0.85)
                    {
                        matches.Add(
                            new DetailedMatch
                            {
                                Id = i + 2000,
                                Type = "Fuzzy Line Match",
                                LeftLines = new List<int> { i + 1, i + 1 },
                                RightLines = new List<int> { j + 1, j + 1 },
                                Severity = "med",
                            }
                        );
                        matchCount++;
                        break;
                    }
                }
            }

            if (validSourceLines == 0)
            {
                similarityScore = 0.0;
            }
            else
            {
                similarityScore = (double)matchCount / validSourceLines * 100;
            }

            return matches;
        }

        private int ComputeDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;

            if (n == 0)
                return m;
            if (m == 0)
                return n;

            int[] v0 = new int[m + 1];
            int[] v1 = new int[m + 1];

            for (int i = 0; i <= m; i++)
            {
                v0[i] = i;
            }

            for (int i = 0; i < n; i++)
            {
                v1[0] = i + 1;

                for (int j = 0; j < m; j++)
                {
                    int cost = (s[i] == t[j]) ? 0 : 1;
                    v1[j + 1] = Math.Min(Math.Min(v1[j] + 1, v0[j + 1] + 1), v0[j] + cost);
                }

                int[] temp = v0;
                v0 = v1;
                v1 = temp;
            }

            return v0[m];
        }
    }
}