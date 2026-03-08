using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Interfaces
{
    public interface IGraphAnalysisService
    {
        AnalysisResponseDto Analyze(GraphAnalysisRequest request);
    }
}
