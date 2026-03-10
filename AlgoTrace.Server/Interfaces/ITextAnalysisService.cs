using AlgoTrace.Server.Models.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrace.Server.Interfaces
{
    public interface ITextAnalysisService
    {
        AnalysisResponse Analyze(AnalysisRequest request);
    }
}
