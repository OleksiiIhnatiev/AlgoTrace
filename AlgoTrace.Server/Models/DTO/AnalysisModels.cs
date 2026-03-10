using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrace.Server.Models.DTO
{
    public class AnalysisRequest
    {
        public string Language { get; set; } = "csharp";
        public SubmissionData SubmissionA { get; set; } = new();
        public SubmissionData SubmissionB { get; set; } = new();
        public AnalysisConfig AnalysisConfig { get; set; } = new();
    }

    public class FileData
    {
        public string Filename { get; set; } = "";
        public string Content { get; set; } = "";
    }

    public class AnalysisConfig
    {
        public List<string> Methods { get; set; } = new();
        public Dictionary<string, bool> Parameters { get; set; } = new();
    }

    public class AnalysisResponse
    {
        public AnalysisInfo Info { get; set; }
        public List<NodeDto> SubmissionTree { get; set; }
        public List<NodeDto> ReferenceTree { get; set; }
    }

    public class AnalysisInfo
    {
        public int OverallScore { get; set; }
        public string Mode { get; set; } = "";
        public string Date { get; set; } = "";
    }

    public class NodeDto
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "file";
        public List<NodeDto>? Children { get; set; }

        //Fields for nodes with "file" type
        public int? Score { get; set; }
        public string? Path { get; set; }
        public Dictionary<string, int>? ReferenceScores { get; set; }
        public Dictionary<string, List<DetailedMatch>>? DetailedMatches { get; set; }
    }
}
