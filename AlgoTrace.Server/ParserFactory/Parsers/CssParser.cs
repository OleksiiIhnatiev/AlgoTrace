using System.Text.RegularExpressions;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.Tree;

namespace AlgoTrace.Server.ParserFactory.Parsers
{
    public class CssParser : ICodeParser
    {
        public string Language => "css";

        public UniversalNode Parse(string code)
        {
            var root = new UniversalNode { Type = "StyleSheet", Value = "CSS" };

            code = SanitizeCssCode(code);

            var ruleRegex = new Regex(@"([^{]+)\{([^}]+)\}", RegexOptions.Compiled);
            var matches = ruleRegex.Matches(code);

            foreach (Match match in matches)
            {
                var selector = match.Groups[1].Value.Trim();
                var body = match.Groups[2].Value.Trim();

                var ruleNode = new UniversalNode { Type = "Rule", Value = selector };

                var properties = body.Split(';');
                foreach (var prop in properties)
                {
                    if (string.IsNullOrWhiteSpace(prop))
                        continue;

                    var parts = prop.Split(':');
                    if (parts.Length == 2)
                    {
                        ruleNode.Children.Add(
                            new UniversalNode { Type = "Property", Value = parts[0].Trim() }
                        );
                    }
                }

                root.Children.Add(ruleNode);
            }

            return root;
        }

        private string SanitizeCssCode(string code)
        {
            var pattern = @"(""(?:\\.|[^\\""])*""|'(?:\\.|[^\\'])*'|/\*[\s\S]*?\*/)";
            return Regex.Replace(
                code,
                pattern,
                match =>
                {
                    if (match.Value.StartsWith("/*"))
                        return "";
                    return "\"STR\"";
                },
                RegexOptions.Multiline
            );
        }
    }
}
