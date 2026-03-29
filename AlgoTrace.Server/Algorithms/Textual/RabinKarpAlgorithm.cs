using System;
using System.Collections.Generic;
using System.Linq;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO.Analysis;
using AlgoTrace.Server.Utils;

namespace AlgoTrace.Server.Algorithms.Textual
{
    public class RabinKarpAlgorithm : ITextAlgorithm
    {
        public string Key => "rabin_karp";
        public string Name => "Rabin-Karp Block Search";

        public List<DetailedMatch> Execute(string source, string target, out double similarityScore)
        {
            var sLines = SourceNormalizer.GetLines(source);
            var tLines = SourceNormalizer.GetLines(target);
            var matches = new List<DetailedMatch>();
            int blockSize = 5;
            int matchCounter = 0;

            if (sLines.Length == 0 || tLines.Length == 0)
            {
                similarityScore = 0.0;
                return matches;
            }

            var sNorms = new string[sLines.Length];
            bool[] isValidSourceLine = new bool[sLines.Length];
            int validSourceLines = 0;

            for (int i = 0; i < sLines.Length; i++)
            {
                sNorms[i] = SourceNormalizer.NormalizeLine(sLines[i]);
                if (sNorms[i].Length >= 5)
                {
                    isValidSourceLine[i] = true;
                    validSourceLines++;
                }
            }

            var tNorms = new string[tLines.Length];
            for (int j = 0; j < tLines.Length; j++)
            {
                tNorms[j] = SourceNormalizer.NormalizeLine(tLines[j]);
            }

            bool[] isMatched = new bool[sLines.Length];
            int lastReportedSourceEnd = -1;

            for (int i = 0; i <= sLines.Length - blockSize; i++)
            {
                var block = string.Join("\n", sNorms.Skip(i).Take(blockSize));

                if (block.Length < 25)
                    continue;

                bool foundMatch = false;
                int bestJ = -1;

                for (int j = 0; j <= tLines.Length - blockSize; j++)
                {
                    var targetBlock = string.Join("\n", tNorms.Skip(j).Take(blockSize));

                    if (block == targetBlock)
                    {
                        foundMatch = true;
                        bestJ = j;
                        break;
                    }
                }

                if (foundMatch)
                {
                    for (int k = 0; k < blockSize; k++)
                    {
                        if (isValidSourceLine[i + k])
                        {
                            isMatched[i + k] = true;
                        }
                    }

                    if (i > lastReportedSourceEnd)
                    {
                        matches.Add(
                            new DetailedMatch
                            {
                                Id = 4000 + matchCounter++,
                                Type = "Exact Block Match",
                                LeftLines = new List<int> { i + 1, i + blockSize },
                                RightLines = new List<int> { bestJ + 1, bestJ + blockSize },
                                Severity = "high",
                            }
                        );
                        lastReportedSourceEnd = i + blockSize - 1;
                    }
                }
            }

            if (validSourceLines == 0)
            {
                similarityScore = 0.0;
            }
            else
            {
                int matchedValidLinesCount = 0;
                for (int i = 0; i < sLines.Length; i++)
                {
                    if (isMatched[i])
                    {
                        matchedValidLinesCount++;
                    }
                }

                similarityScore = Math.Min(
                    100.0,
                    (double)matchedValidLinesCount / validSourceLines * 100
                );
            }

            return matches;
        }
    }
}