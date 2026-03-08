using System.Text.Json.Serialization;

namespace AlgoTrace.Server.Models.DTO
{
    public class GraphAnalysisRequest
    {
        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("submission_a")]
        public SubmissionDto SubmissionA { get; set; }

        [JsonPropertyName("submission_b")]
        public SubmissionDto SubmissionB { get; set; }

        [JsonPropertyName("analysis_config")]
        public AnalysisConfigDto AnalysisConfig { get; set; }
    }

    public class SubmissionDto
    {
        [JsonPropertyName("author_id")]
        public string AuthorId { get; set; }

        [JsonPropertyName("files")]
        public List<FileItemDto> Files { get; set; }
    }

    public class FileItemDto
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class AnalysisConfigDto
    {
        [JsonPropertyName("methods")]
        public List<string> Methods { get; set; }

        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; }
    }
}
