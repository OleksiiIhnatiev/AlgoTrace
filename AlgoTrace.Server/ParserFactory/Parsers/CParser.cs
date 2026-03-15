using System.Text.RegularExpressions;
using AlgoTrace.Server.Models.Tree;
using AlgoTrace.Server.ParserFactory.Parsers.Base;

namespace AlgoTrace.Server.ParserFactory.Parsers
{
    public class CParser : BraceLanguageParser
    {
        public override string Language => "c";

        protected override UniversalNode IdentifyNode(string line)
        {
            var node = new UniversalNode { Type = UniversalNodeType.Unknown, Value = "" };

            if (line.StartsWith("#")) return node;

            if (line.Contains("struct "))
            {
                node.Type = UniversalNodeType.Class;
                node.Value = Regex.Match(line, @"struct\s+(\w+)").Groups[1].Value;
            }
            else if (Regex.IsMatch(line, @"^\s*[\w]+[\*\&]?\s+(\w+)\s*\("))
            {
                node.Type = UniversalNodeType.Method;
                node.Value = Regex.Match(line, @"(\w+)\s*\(").Groups[1].Value;
            }
            else if (line.StartsWith("if") || line.StartsWith("else"))
                node.Type = UniversalNodeType.If;
            else if (line.StartsWith("for") || line.StartsWith("while") || line.StartsWith("do "))
                node.Type = UniversalNodeType.Loop;
            else if (line.StartsWith("switch"))
                node.Type = UniversalNodeType.Switch;
            else if (line.StartsWith("return"))
                node.Type = UniversalNodeType.Return;
            else if (line.Contains("=") && !line.Contains("=="))
                node.Type = UniversalNodeType.Assignment;

            return node;
        }
    }
}