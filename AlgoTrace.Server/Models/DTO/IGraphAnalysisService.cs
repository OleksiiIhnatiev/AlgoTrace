namespace AlgoTrace.Server.Models.DTO
{
    public interface IGraphAnalysisService
    {
        AnalysisResponseDto Analyze(GraphAnalysisRequest request);
    }
}
