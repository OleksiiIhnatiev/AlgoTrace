using System.Text.RegularExpressions;
using AlgoTrace.Server.Models.Tree;
using AlgoTrace.Server.ParserFactory.Parsers.Base;

namespace AlgoTrace.Server.ParserFactory.Parsers
{
    public class JavaScriptParser : BraceLanguageParser
    {
        public override string Language => "javascript";

        protected override UniversalNode IdentifyNode(string line)
        {
            var node = new UniversalNode { Type = UniversalNodeType.Unknown, Value = "" };

            if (line.StartsWith("class "))
            {
                node.Type = UniversalNodeType.Class;
                node.Value = Regex.Match(line, @"class\s+(\w+)").Groups[1].Value;
            }
            else if (Regex.IsMatch(line, @"function\s+(\w+)\s*\("))
            {
                node.Type = UniversalNodeType.Method;
                node.Value = Regex.Match(line, @"function\s+(\w+)").Groups[1].Value;
            }
            else if (
                line.Contains("=>")
                && (line.Contains("const") || line.Contains("let") || line.Contains("var"))
            )
            {
                node.Type = UniversalNodeType.Method;
                node.Value = Regex.Match(line, @"(?:const|let|var)\s+(\w+)").Groups[1].Value;
            }
            else if (line.StartsWith("if") || line.StartsWith("else"))
                node.Type = UniversalNodeType.If;
            else if (
                line.StartsWith("for")
                || line.StartsWith("while")
                || line.Contains(".forEach(")
            )
                node.Type = UniversalNodeType.Loop;
            else if (line.StartsWith("return "))
                node.Type = UniversalNodeType.Return;
            else if (
                line.StartsWith("try")
                || line.StartsWith("catch")
                || line.StartsWith("finally")
            )
                node.Type = UniversalNodeType.TryCatch;
            else if (
                (line.Contains("const ") || line.Contains("let ") || line.Contains("var "))
                && line.Contains("=")
            )
                node.Type = UniversalNodeType.VariableDecl;
            else if (line.Contains("=") && !line.Contains("=="))
                node.Type = UniversalNodeType.Assignment;

            return node;
        }
    }
}
