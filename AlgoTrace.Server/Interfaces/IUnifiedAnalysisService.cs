using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Interfaces
{
    public interface IUnifiedAnalysisService
    {
        UnifiedAnalysisResponse Analyze(UnifiedAnalysisRequest request);
    }
}
