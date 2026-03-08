using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Interfaces
{
    public interface IMetricAlgorithm
    {
        string Key { get; }
        string Name { get; }
        List<DetailedMatch> Execute(
            string sourceCode,
            string targetCode,
            Dictionary<string, object> parameters,
            out double similarityScore
        );
    }
}
