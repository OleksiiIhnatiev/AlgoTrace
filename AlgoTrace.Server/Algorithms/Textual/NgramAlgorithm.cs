using System;
using System.Collections.Generic;
using System.Linq;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO.Analysis;
using AlgoTrace.Server.Utils;

namespace AlgoTrace.Server.Algorithms.Textual
{
    public class NgramAlgorithm : ITextAlgorithm
    {
        public string Key => "ngram_search";
        public string Name => "N-Gram Fragment Search";

        public List<DetailedMatch> Execute(string source, string target, out double similarityScore)
        {
            var matches = new List<DetailedMatch>();
            var sLines = SourceNormalizer.GetLines(source);
            var tLines = SourceNormalizer.GetLines(target);

            if (sLines.Length == 0 || tLines.Length == 0)
            {
                similarityScore = 0;
                return matches;
            }

            string[] sNorms = new string[sLines.Length];
            for (int i = 0; i < sLines.Length; i++)
                sNorms[i] = SourceNormalizer.NormalizeLine(sLines[i]);

            string[] tNorms = new string[tLines.Length];
            for (int j = 0; j < tLines.Length; j++)
                tNorms[j] = SourceNormalizer.NormalizeLine(tLines[j]);

            double[] lineMaxScores = new double[sLines.Length];
            bool[] isLineEvaluated = new bool[sLines.Length];

            int matchCounter = 0;
            int lastReportedSourceEnd = -1;

            for (int i = 0; i <= sLines.Length - 4; i++)
            {
                string sBlock = string.Join("", sNorms.Skip(i).Take(4));
                if (sBlock.Length < 20)
                    continue;

                for (int k = 0; k < 4; k++)
                    isLineEvaluated[i + k] = true;

                double bestScore = 0;
                int bestJ = -1;

                for (int j = 0; j <= tLines.Length - 4; j++)
                {
                    string tBlock = string.Join("", tNorms.Skip(j).Take(4));
                    double score = CalculateJaccard(sBlock, tBlock);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestJ = j;
                    }
                }

                for (int k = 0; k < 4; k++)
                {
                    if (bestScore > lineMaxScores[i + k])
                    {
                        lineMaxScores[i + k] = bestScore;
                    }
                }

                if (bestScore >= 0.7 && i > lastReportedSourceEnd)
                {
                    matches.Add(
                        new DetailedMatch
                        {
                            Id = 3000 + matchCounter++,
                            Type = "Structural Fragment Match",
                            LeftLines = new List<int> { i + 1, i + 4 },
                            RightLines = new List<int> { bestJ + 1, bestJ + 4 },
                            Severity = "med",
                        }
                    );
                    lastReportedSourceEnd = i + 3;
                }
            }

            double totalScore = 0;
            int evaluatedCount = 0;

            for (int i = 0; i < sLines.Length; i++)
            {
                if (isLineEvaluated[i])
                {
                    totalScore += lineMaxScores[i];
                    evaluatedCount++;
                }
            }

            similarityScore = evaluatedCount > 0 ? (totalScore / evaluatedCount) * 100 : 0;
            return matches;
        }

        private double CalculateJaccard(string s1, string s2)
        {
            var n1 = GetGrams(s1);
            var n2 = GetGrams(s2);
            if (n1.Count == 0 || n2.Count == 0)
                return 0;

            int intersection = 0;
            foreach (var item in n1)
            {
                if (n2.Contains(item))
                    intersection++;
            }

            int union = n1.Count + n2.Count - intersection;
            return (double)intersection / union;
        }

        private HashSet<string> GetGrams(string text)
        {
            var set = new HashSet<string>();
            for (int i = 0; i <= text.Length - 4; i++)
                set.Add(text.Substring(i, 4));
            return set;
        }
    }
}