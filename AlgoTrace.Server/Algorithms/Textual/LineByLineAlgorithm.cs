using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO.Analysis;
using AlgoTrace.Server.Utils;

namespace AlgoTrace.Server.Algorithms.Textual
{
    public class LineByLineAlgorithm : ITextAlgorithm
    {
        public string Key => "line_matching";
        public string Name => "Line Matching";

        public List<DetailedMatch> Execute(string source, string target, out double similarityScore)
        {
            var sLines = SourceNormalizer.GetLines(source);
            var tLines = SourceNormalizer.GetLines(target);
            var rawMatches = new List<(int sIdx, int tIdx)>();

            for (int i = 0; i < sLines.Length; i++)
            {
                string sNorm = SourceNormalizer.NormalizeLine(sLines[i]);
                if (sNorm.Length < 8)
                    continue;

                for (int j = 0; j < tLines.Length; j++)
                {
                    if (sNorm == SourceNormalizer.NormalizeLine(tLines[j]))
                    {
                        rawMatches.Add((i + 1, j + 1));
                        break;
                    }
                }
            }

            var merged = new List<DetailedMatch>();
            if (!rawMatches.Any())
            {
                similarityScore = 0;
                return merged;
            }

            int startS = rawMatches[0].sIdx;
            int startT = rawMatches[0].tIdx;
            int lastS = startS;
            int lastT = startT;
            int matchCounter = 1;

            for (int i = 1; i < rawMatches.Count; i++)
            {
                if (rawMatches[i].sIdx == lastS + 1 && rawMatches[i].tIdx == lastT + 1)
                {
                    lastS = rawMatches[i].sIdx;
                    lastT = rawMatches[i].tIdx;
                }
                else
                {
                    merged.Add(
                        new DetailedMatch
                        {
                            Id = 1000 + matchCounter++,
                            Type = "Code Block Clone",
                            LeftLines = new List<int> { startS, lastS },
                            RightLines = new List<int> { startT, lastT },
                            Severity = (lastS - startS) > 3 ? "high" : "med",
                        }
                    );
                    startS = rawMatches[i].sIdx;
                    startT = rawMatches[i].tIdx;
                    lastS = startS;
                    lastT = startT;
                }
            }

            merged.Add(
                new DetailedMatch
                {
                    Id = 1000 + matchCounter,
                    Type = "Code Block Clone",
                    LeftLines = new List<int> { startS, lastS },
                    RightLines = new List<int> { startT, lastT },
                    Severity = (lastS - startS) > 3 ? "high" : "med",
                }
            );

            similarityScore =
                (double)rawMatches.Count / Math.Max(sLines.Length, tLines.Length) * 100;
            return merged;
        }
    }
}
