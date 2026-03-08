using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Algorithms.Metric
{
    public class HalsteadMetricsAlgorithm : IMetricAlgorithm
    {
        public string Key => "halstead";
        public string Name => "Halstead Metrics Comparison";

        public List<DetailedMatch> Execute(
            string sourceCode,
            string targetCode,
            Dictionary<string, object> parameters,
            out double similarityScore
        )
        {
            var metricsA = MetricUtils.CalculateHalsteadMetrics(sourceCode);
            var metricsB = MetricUtils.CalculateHalsteadMetrics(targetCode);

            double v1 = metricsA.Volume;
            double v2 = metricsB.Volume;

            double maxV = Math.Max(v1, v2);
            if (maxV == 0)
                maxV = 1;

            double diff = Math.Abs(v1 - v2);
            similarityScore = Math.Max(0, (1 - (diff / maxV)) * 100);

            var matches = new List<DetailedMatch>();

            if (similarityScore > 70)
            {
                int linesA = sourceCode.Split('\n').Length;
                int linesB = targetCode.Split('\n').Length;

                matches.Add(
                    new DetailedMatch
                    {
                        Id = 8001,
                        Type = $"Halstead Volume Match (A:{v1:F1}, B:{v2:F1})",
                        LeftLines = new List<int> { 1, linesA },
                        RightLines = new List<int> { 1, linesB },
                        Severity = similarityScore > 90 ? "high" : "med",
                    }
                );
            }

            return matches;
        }
    }
}
