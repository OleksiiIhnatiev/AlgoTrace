using System.Text.RegularExpressions;
using AlgoTrace.Server.Models.Tree;
using AlgoTrace.Server.ParserFactory.Parsers.Base;

namespace AlgoTrace.Server.ParserFactory.Parsers
{
    public class RustParser : BraceLanguageParser
    {
        public override string Language => "rust";

        protected override UniversalNode IdentifyNode(string line)
        {
            var node = new UniversalNode { Type = UniversalNodeType.Unknown, Value = "" };

            if (line.StartsWith("struct ") || line.StartsWith("enum ") || line.StartsWith("impl "))
            {
                node.Type = UniversalNodeType.Class;
                node.Value = Regex.Match(line, @"(?:struct|enum|impl)\s+([\w<>]+)").Groups[1].Value;
            }
            else if (line.StartsWith("fn ") || line.StartsWith("pub fn "))
            {
                node.Type = UniversalNodeType.Method;
                node.Value = Regex.Match(line, @"fn\s+(\w+)\s*\(").Groups[1].Value;
            }
            else if (line.StartsWith("if ") || line.StartsWith("else "))
                node.Type = UniversalNodeType.If;
            else if (
                line.StartsWith("for ")
                || line.StartsWith("while ")
                || line.StartsWith("loop ")
            )
                node.Type = UniversalNodeType.Loop;
            else if (line.StartsWith("match "))
                node.Type = UniversalNodeType.Switch;
            else if (line.StartsWith("return "))
                node.Type = UniversalNodeType.Return;
            else if (line.StartsWith("let ") && line.Contains("="))
                node.Type = UniversalNodeType.VariableDecl;
            else if (line.Contains("=") && !line.Contains("=="))
                node.Type = UniversalNodeType.Assignment;

            return node;
        }
    }
}
