using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlgoTrace.Server.Models.DTO
{
    public class UnifiedAnalysisRequest
    {
        [JsonPropertyName("language")]
        public string Language { get; set; } = "python";

        [JsonPropertyName("submission_a")]
        public SubmissionData SubmissionA { get; set; } = new();

        [JsonPropertyName("submission_b")]
        public SubmissionData SubmissionB { get; set; } = new();

        [JsonPropertyName("analysis_config")]
        public UnifiedConfig AnalysisConfig { get; set; } = new();
    }

    public class UnifiedConfig
    {
        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new();

        [JsonPropertyName("execute_categories")]
        public ExecuteCategories ExecuteCategories { get; set; } = new();
    }

    public class ExecuteCategories
    {
        [JsonPropertyName("text_based")]
        public List<string> TextBased { get; set; } = new();

        [JsonPropertyName("token_based")]
        public List<string> TokenBased { get; set; } = new();

        [JsonPropertyName("tree_based")]
        public List<string> TreeBased { get; set; } = new();

        [JsonPropertyName("graph_based")]
        public List<string> GraphBased { get; set; } = new();

        [JsonPropertyName("metrics_based")]
        public List<string> MetricsBased { get; set; } = new();
    }

    public class UnifiedAnalysisResponse
    {
        [JsonPropertyName("analysis_id")]
        public string AnalysisId { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "completed";

        [JsonPropertyName("global_similarity_score")]
        public double GlobalSimilarityScore { get; set; }

        [JsonPropertyName("categories_results")]
        public List<CategoryResult> CategoriesResults { get; set; } = new();

        [JsonPropertyName("source_files")]
        public SourceFilesInfo SourceFiles { get; set; } = new();
    }

    public class SourceFilesInfo
    {
        [JsonPropertyName("file_a")]
        public string FileA { get; set; } = "";

        [JsonPropertyName("file_b")]
        public string FileB { get; set; } = "";

        [JsonPropertyName("name_a")]
        public string NameA { get; set; } = "";

        [JsonPropertyName("name_b")]
        public string NameB { get; set; } = "";
    }

    public class CategoryResult
    {
        [JsonPropertyName("category_name")]
        public string CategoryName { get; set; } = "";

        [JsonPropertyName("category_similarity_score")]
        public double CategorySimilarityScore { get; set; }

        [JsonPropertyName("algorithms")]
        public List<AlgorithmResult> Algorithms { get; set; } = new();
    }

    public class AlgorithmResult
    {
        [JsonPropertyName("method")]
        public string Method { get; set; } = "";

        [JsonPropertyName("similarity_score")]
        public double SimilarityScore { get; set; }

        [JsonPropertyName("evidence_type")]
        public string EvidenceType { get; set; } = "";

        [JsonPropertyName("evidence")]
        public object Evidence { get; set; } = new { };
    }

    // Specific Evidence Models
    public class TextEvidence
    {
        [JsonPropertyName("matched_blocks")]
        public List<object> MatchedBlocks { get; set; } = new();
    }

    public class TokenEvidence
    {
        [JsonPropertyName("matched_hashes")]
        public List<object> MatchedHashes { get; set; } = new();
    }

    public class MetricEvidence
    {
        [JsonPropertyName("metrics_a")]
        public Dictionary<string, double> MetricsA { get; set; } = new();

        [JsonPropertyName("metrics_b")]
        public Dictionary<string, double> MetricsB { get; set; } = new();

        [JsonPropertyName("conclusion")]
        public string Conclusion { get; set; } = "";
    }

    public class TreeEvidence
    {
        [JsonPropertyName("matched_subtrees")]
        public List<MatchedSubtree> MatchedSubtrees { get; set; } = new();
    }

    public class MatchedSubtree
    {
        [JsonPropertyName("node_type")]
        public string NodeType { get; set; } = "";

        [JsonPropertyName("nodes_a")]
        public List<MatchedNodeInfo> NodesA { get; set; } = new();

        [JsonPropertyName("nodes_b")]
        public List<MatchedNodeInfo> NodesB { get; set; } = new();
    }

    public class MatchedNodeInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("label")]
        public string Label { get; set; } = "";

        [JsonPropertyName("children")]
        public List<string> Children { get; set; } = new();

        [JsonPropertyName("location")]
        public CodeLocation Location { get; set; } = new();
    }

    public class CodeLocation
    {
        [JsonPropertyName("start_line")]
        public int StartLine { get; set; }

        [JsonPropertyName("start_column")]
        public int StartColumn { get; set; }

        [JsonPropertyName("end_line")]
        public int EndLine { get; set; }

        [JsonPropertyName("end_column")]
        public int EndColumn { get; set; }
    }

    // For graph/tree we can use generic objects or Dictionary<string,object>
    // as the visualization structure can be complex.

    public class MultiDocumentAnalysisRequest
    {
        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("submission")]
        public SubmissionData Submission { get; set; }

        [JsonPropertyName("compare_with_document_ids")]
        public List<string> CompareWithDocumentIds { get; set; }

        [JsonPropertyName("analysis_config")]
        public AnalysisConfigMultiDoc AnalysisConfig { get; set; }
    }

    public class AnalysisConfigMultiDoc
    {
        [JsonPropertyName("categories")]
        public List<CategoryMethods> Categories { get; set; }

        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; }
    }

    public class CategoryMethods
    {
        [JsonPropertyName("category_name")]
        public string CategoryName { get; set; }

        [JsonPropertyName("methods")]
        public List<string> Methods { get; set; }
    }

    public class MultiDocumentAnalysisResponse
    {
        [JsonPropertyName("analysis_id")]
        public string AnalysisId { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "completed";

        [JsonPropertyName("main_submission")]
        public CodeFile MainSubmission { get; set; } = new();

        [JsonPropertyName("results")]
        public List<DocumentComparisonResult> Results { get; set; } = new();
    }

    public class DocumentComparisonResult
    {
        [JsonPropertyName("document_id")]
        public string DocumentId { get; set; } = "";

        [JsonPropertyName("target_file")]
        public CodeFile TargetFile { get; set; } = new();

        [JsonPropertyName("global_similarity_score")]
        public double GlobalSimilarityScore { get; set; }

        [JsonPropertyName("categories_results")]
        public List<CategoryResult> CategoriesResults { get; set; } = new();

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
