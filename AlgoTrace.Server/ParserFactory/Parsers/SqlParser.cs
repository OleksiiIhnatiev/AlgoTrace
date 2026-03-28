using System.Text.RegularExpressions;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.Tree;

namespace AlgoTrace.Server.ParserFactory.Parsers
{
    public class SqlParser : ICodeParser
    {
        public string Language => "sql";

        public UniversalNode Parse(string code)
        {
            var root = new UniversalNode { Type = UniversalNodeType.Program, Value = "SQLScript" };
            var sanitizedCode = SanitizeSqlCode(code);
            var statements = sanitizedCode.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var stmt in statements)
            {
                var trimmed = stmt.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                var firstWord = trimmed.Split(' ').FirstOrDefault()?.ToUpper();

                root.Children.Add(
                    new UniversalNode { Type = "Statement", Value = firstWord ?? "QUERY" }
                );
            }
            return root;
        }

        private string SanitizeSqlCode(string code)
        {
            var pattern = @"(""(?:\\.|[^\\""])*""|'(?:\\.|[^\\'])*'|--.*?$|/\*[\s\S]*?\*/)";
            return Regex.Replace(
                code,
                pattern,
                match =>
                {
                    if (match.Value.StartsWith("--") || match.Value.StartsWith("/*"))
                        return "";
                    return "'STR'";
                },
                RegexOptions.Multiline
            );
        }
    }
}
