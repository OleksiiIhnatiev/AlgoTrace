using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;
using AlgoTrace.Server.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrace.Server.Algorithms
{
    public class RabinKarpAlgorithm : ITextAlgorithm
    {
        public string Name => "Rabin-Karp Block Search";

        public List<DetailedMatch> Execute(string source, string target, out double similarityScore)
        {
            var sLines = SourceNormalizer.GetLines(source);
            var tLines = SourceNormalizer.GetLines(target);
            var matches = new List<DetailedMatch>();
            int blockSize = 5;
            int matchedLinesCount = 0;

            for (int i = 0; i <= sLines.Length - blockSize; i++)
            {
                var block = string.Join(
                    "\n",
                    sLines.Skip(i).Take(blockSize).Select(SourceNormalizer.NormalizeLine)
                );
                if (block.Length < 50)
                    continue;

                for (int j = 0; j <= tLines.Length - blockSize; j++)
                {
                    var targetBlock = string.Join(
                        "\n",
                        tLines.Skip(j).Take(blockSize).Select(SourceNormalizer.NormalizeLine)
                    );

                    if (block == targetBlock)
                    {
                        matches.Add(
                            new DetailedMatch
                            {
                                Id = new Random().Next(100, 999),
                                Type = "Exact Block Match",
                                LeftLines = new List<int> { i + 1, i + blockSize },
                                RightLines = new List<int> { j + 1, j + blockSize },
                                Severity = "high",
                            }
                        );
                        matchedLinesCount += blockSize;
                        i += blockSize - 1;
                        break;
                    }
                }
            }

            similarityScore =
                (double)matchedLinesCount / Math.Max(sLines.Length, tLines.Length) * 100;
            return matches;
        }
    }
}
