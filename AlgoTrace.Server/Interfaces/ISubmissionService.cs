using AlgoTrace.Server.Models.DTO.Analysis;

namespace AlgoTrace.Server.Interfaces
{
    public interface ISubmissionService
    {
        Task<SubmissionAnalysisResponse> ProcessSubmissionAsync(Stream fileStream, string fileName);
    }
}
