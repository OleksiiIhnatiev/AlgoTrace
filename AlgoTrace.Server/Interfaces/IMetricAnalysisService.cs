using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Interfaces
{
    public interface IMetricAnalysisService
    {
        AnalysisResponseDto Analyze(MetricAnalysisRequest request);
    }
}
