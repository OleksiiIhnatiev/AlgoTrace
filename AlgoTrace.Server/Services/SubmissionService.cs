using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO.Analysis;

namespace AlgoTrace.Server.Services
{
    public class SubmissionService : ISubmissionService
    {
        public async Task<SubmissionAnalysisResponse> ProcessSubmissionAsync(
            Stream fileStream,
            string fileName
        )
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var language = GetLanguageFromExtension(extension);

            if (language == "unknown")
            {
                throw new ArgumentException(
                    $"Unsupported file type: {extension}. Please upload a valid code file."
                );
            }

            string content;
            using (var reader = new StreamReader(fileStream))
            {
                content = await reader.ReadToEndAsync();
            }

            return new SubmissionAnalysisResponse
            {
                Language = language,
                Submission = new SubmissionData
                {
                    Files = new List<CodeFile>
                    {
                        new CodeFile { Filename = fileName, Content = content },
                    },
                },
            };
        }

        private string GetLanguageFromExtension(string extension)
        {
            return extension switch
            {
                ".cs" => "csharp",
                ".py" => "python",
                ".java" => "java",
                ".kt" or ".kts" => "kotlin",
                ".js" or ".jsx" => "javascript",
                ".ts" or ".tsx" => "typescript",
                ".c" => "c",
                ".cpp" or ".h" or ".hpp" or ".m" => "cpp",
                ".go" => "go",
                ".rs" => "rust",
                ".swift" => "swift",
                ".rb" => "ruby",
                ".php" => "php",
                ".sql" => "sql",
                ".html" => "html",
                ".xml" => "xml",
                ".css" or ".scss" or ".sass" or ".less" => "css",
                ".json" => "json",
                ".yaml" or ".yml" => "yaml",
                ".sh" or ".bash" or ".ps1" or ".bat" => "bash",
                _ => "unknown",
            };
        }
    }
}