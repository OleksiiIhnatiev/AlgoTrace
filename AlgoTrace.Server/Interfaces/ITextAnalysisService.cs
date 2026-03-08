using AlgoTrace.Server.Models.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrace.Server.Interfaces
{
    public interface ITextAnalysisService
    {
        AnalysisResponseDto Analyze(
            string source,
            string reference,
            string sourcePath,
            string refPath
        );
    }
}
