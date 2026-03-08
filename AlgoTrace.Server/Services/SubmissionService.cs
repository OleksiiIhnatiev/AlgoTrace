using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;

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
                ".py" => "python",
                ".cs" => "csharp",
                ".cpp" => "cpp",
                ".c" => "c",
                ".java" => "java",
                ".js" => "javascript",
                ".ts" => "typescript",
                ".go" => "go",
                ".rb" => "ruby",
                ".php" => "php",
                ".html" => "html",
                ".css" => "css",
                _ => "unknown",
            };
        }
    }
}
