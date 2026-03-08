using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Algorithms.Metric
{
    public class McCabeComplexityAlgorithm : IMetricAlgorithm
    {
        public string Key => "mccabe";
        public string Name => "McCabe Cyclomatic Complexity";

        public List<DetailedMatch> Execute(
            string sourceCode,
            string targetCode,
            Dictionary<string, object> parameters,
            out double similarityScore
        )
        {
            int c1 = MetricUtils.CalculateMcCabeComplexity(sourceCode);
            int c2 = MetricUtils.CalculateMcCabeComplexity(targetCode);

            double maxC = Math.Max(c1, c2);
            if (maxC == 0)
                maxC = 1;

            double diff = Math.Abs(c1 - c2);
            similarityScore = Math.Max(0, (1 - (diff / maxC)) * 100);

            var matches = new List<DetailedMatch>();

            if (similarityScore > 80)
            {
                int linesA = sourceCode.Split('\n').Length;
                int linesB = targetCode.Split('\n').Length;

                matches.Add(
                    new DetailedMatch
                    {
                        Id = 9001,
                        Type = $"McCabe Complexity Match (A:{c1}, B:{c2})",
                        LeftLines = new List<int> { 1, linesA },
                        RightLines = new List<int> { 1, linesB },
                        Severity = similarityScore > 95 ? "high" : "med",
                    }
                );
            }

            return matches;
        }
    }
}
