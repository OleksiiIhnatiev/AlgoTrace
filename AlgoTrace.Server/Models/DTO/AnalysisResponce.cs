using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrace.Server.Models.DTO
{
    public record AnalysisRequest(
        string SourceCode,
        string ReferenceCode,
        string SourcePath,
        string ReferencePath
    );

    public class AnalysisResponseDto
    {
        public AnalysisInfo Info { get; set; }
        public List<NodeDto> SubmissionTree { get; set; }
        public List<NodeDto> ReferenceTree { get; set; }
    }
}
