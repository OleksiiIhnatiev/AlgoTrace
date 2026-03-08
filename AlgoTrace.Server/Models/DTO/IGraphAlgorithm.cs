namespace AlgoTrace.Server.Models.DTO
{
    public interface IGraphAlgorithm
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
