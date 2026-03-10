using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Interfaces
{
    public interface IMetricAnalysisService
    {
        AnalysisResponse Analyze(AnalysisRequest request);
    }
}
