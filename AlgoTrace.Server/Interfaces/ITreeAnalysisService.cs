using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Interfaces
{
    public interface ITreeAnalysisService
    {
        AnalysisResponse Analyze(AnalysisRequest request);
    }
}
