using AlgoTrace.Server.Models.DTO.Analysis;

namespace AlgoTrace.Server.Interfaces
{
    public interface IUnifiedAnalysisService
    {
        UnifiedAnalysisResponse Analyze(UnifiedAnalysisRequest request);
    }
}
