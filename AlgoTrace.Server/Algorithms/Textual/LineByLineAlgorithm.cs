using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;
using AlgoTrace.Server.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrace.Server.Algorithms.Textual
{
    public class LineByLineAlgorithm : ITextAlgorithm
    {
        public string Name => "Line Matching";

        public List<DetailedMatch> Execute(string source, string target, out double similarityScore)
        {
            var sLines = SourceNormalizer.GetLines(source);
            var tLines = SourceNormalizer.GetLines(target);
            var rawMatches = new List<(int sIdx, int tIdx)>();

            // 1. Собираем все точки соприкосновения
            for (int i = 0; i < sLines.Length; i++)
            {
                string sNorm = SourceNormalizer.NormalizeLine(sLines[i]);
                if (sNorm.Length < 8)
                    continue; // Игнорируем шум (скобки, пустые строки)

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

            // 2. Группируем идущие подряд строки в один фрагмент
            int startS = rawMatches[0].sIdx;
            int startT = rawMatches[0].tIdx;
            int lastS = startS;
            int lastT = startT;

            for (int i = 1; i < rawMatches.Count; i++)
            {
                // Если следующая совпавшая строка идет сразу за текущей в обоих файлах
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
                            Id = new Random().Next(1000, 9999),
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
            // Добавляем последний найденный блок
            merged.Add(
                new DetailedMatch
                {
                    Id = new Random().Next(1000, 9999),
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
