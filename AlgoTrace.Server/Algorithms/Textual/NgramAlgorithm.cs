using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;
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
            int matchCounter = 0;

            for (int i = 0; i <= sLines.Length - 4; i++)
            {
                var sBlock = string.Join("", sLines.Skip(i).Take(4).Select(SourceNormalizer.NormalizeLine));
                if (sBlock.Length < 20) continue;

                for (int j = 0; j <= tLines.Length - 4; j++)
                {
                    var tBlock = string.Join("", tLines.Skip(j).Take(4).Select(SourceNormalizer.NormalizeLine));
                    double score = CalculateJaccard(sBlock, tBlock);

                    if (score > 0.7)
                    {
                        matches.Add(new DetailedMatch
                        {
                            Id = 3000 + matchCounter++,
                            Type = "Structural Fragment Match",
                            LeftLines = new List<int> { i + 1, i + 4 },
                            RightLines = new List<int> { j + 1, j + 4 },
                            Severity = "med"
                        });
                    }
                }
            }

            similarityScore = matches.Count > 0 ? Math.Min(100, matches.Count * 5) : 0;
            return matches;
        }

        private double CalculateJaccard(string s1, string s2)
        {
            var n1 = GetGrams(s1);
            var n2 = GetGrams(s2);
            if (!n1.Any() || !n2.Any()) return 0;

            return (double)n1.Intersect(n2).Count() / n1.Union(n2).Count();
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