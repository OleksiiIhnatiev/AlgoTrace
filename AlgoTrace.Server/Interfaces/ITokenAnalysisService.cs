using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Interfaces
{
    public interface ITokenAnalysisService
    {
        AnalysisResponse Analyze(AnalysisRequest request);
    }
}
