using System.Text.Json.Serialization;

namespace AlgoTrace.Server.Models.DTO
{
    public class MetricAnalysisRequest
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
}
