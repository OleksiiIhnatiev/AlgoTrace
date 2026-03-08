using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Interfaces
{
    public interface ITokenAnalysisService
    {
        AnalysisResponseDto Analyze(
            string source,
            string reference,
            string sourceName,
            string refName
        );
    }
}
