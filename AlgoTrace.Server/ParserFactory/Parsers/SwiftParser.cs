using System.Text.RegularExpressions;
using AlgoTrace.Server.Models.Tree;
using AlgoTrace.Server.ParserFactory.Parsers.Base;

namespace AlgoTrace.Server.ParserFactory.Parsers
{
    public class SwiftParser : BraceLanguageParser
    {
        public override string Language => "swift";

        protected override UniversalNode IdentifyNode(string line)
        {
            var node = new UniversalNode { Type = UniversalNodeType.Unknown, Value = "" };

            if (
                line.Contains("class ")
                || line.Contains("struct ")
                || line.Contains("enum ")
                || line.Contains("protocol ")
            )
            {
                node.Type = UniversalNodeType.Class;
                node.Value = Regex
                    .Match(line, @"(?:class|struct|enum|protocol)\s+(\w+)")
                    .Groups[1]
                    .Value;
            }
            else if (line.Contains("func "))
            {
                node.Type = UniversalNodeType.Method;
                node.Value = Regex.Match(line, @"func\s+(\w+)\s*\(").Groups[1].Value;
            }
            else if (line.StartsWith("if ") || line.StartsWith("else") || line.StartsWith("guard "))
                node.Type = UniversalNodeType.If;
            else if (
                line.StartsWith("for ")
                || line.StartsWith("while ")
                || line.StartsWith("repeat ")
            )
                node.Type = UniversalNodeType.Loop;
            else if (line.StartsWith("switch "))
                node.Type = UniversalNodeType.Switch;
            else if (line.StartsWith("return "))
                node.Type = UniversalNodeType.Return;
            else if ((line.StartsWith("let ") || line.StartsWith("var ")) && line.Contains("="))
                node.Type = UniversalNodeType.VariableDecl;
            else if (line.Contains("=") && !line.Contains("=="))
                node.Type = UniversalNodeType.Assignment;

            return node;
        }
    }
}
