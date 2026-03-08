using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Interfaces
{
    public interface ITokenAlgorithm
    {
        string Name { get; }
        List<DetailedMatch> Execute(List<TokenInfo> sourceTokens, List<TokenInfo> targetTokens, out double similarityScore);
    }
}