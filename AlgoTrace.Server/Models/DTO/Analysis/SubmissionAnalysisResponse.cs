using System.Text.Json.Serialization;

namespace AlgoTrace.Server.Models.DTO.Analysis
{
    public class SubmissionAnalysisResponse
    {
        [JsonPropertyName("language")]
        public string Language { get; set; } = "unknown";

        [JsonPropertyName("submission")]
        public SubmissionData Submission { get; set; } = new();
    }

    public class SubmissionData
    {
        [JsonPropertyName("files")]
        public List<CodeFile> Files { get; set; } = new();
    }

    public class CodeFile
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }
}
