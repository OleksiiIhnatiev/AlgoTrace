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
            var statements = code.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var stmt in statements)
            {
                var trimmed = stmt.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                var firstWord = trimmed.Split(' ').FirstOrDefault()?.ToUpper();

                root.Children.Add(new UniversalNode
                {
                    Type = "Statement",
                    Value = firstWord ?? "QUERY"
                });
            }
            return root;
        }
    }
}