using AlgoTrace.Server.Models.DTO.Analysis;

namespace AlgoTrace.Server.Interfaces
{
    public interface ITokenAlgorithm
    {
        string Key { get; }
        string Name { get; }
        List<DetailedMatch> Execute(
            List<TokenInfo> sourceTokens,
            List<TokenInfo> targetTokens,
            out double similarityScore
        );
    }
}
