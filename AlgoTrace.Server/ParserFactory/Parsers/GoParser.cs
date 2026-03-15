using System.Text.RegularExpressions;
using AlgoTrace.Server.Models.Tree;
using AlgoTrace.Server.ParserFactory.Parsers.Base;

namespace AlgoTrace.Server.ParserFactory.Parsers
{
    public class GoParser : BraceLanguageParser
    {
        public override string Language => "go";

        protected override UniversalNode IdentifyNode(string line)
        {
            var node = new UniversalNode { Type = UniversalNodeType.Unknown, Value = "" };

            if (line.StartsWith("package ") || line.StartsWith("import ")) return node;

            if (line.StartsWith("type ") && line.Contains("struct"))
            {
                node.Type = UniversalNodeType.Class;
                node.Value = Regex.Match(line, @"type\s+(\w+)\s+struct").Groups[1].Value;
            }
            else if (line.StartsWith("func "))
            {
                node.Type = UniversalNodeType.Method;
                var match = Regex.Match(line, @"func\s+(?:\([^\)]+\)\s+)?(\w+)\s*\(");
                node.Value = match.Groups[1].Value;
            }
            else if (line.StartsWith("if "))
                node.Type = UniversalNodeType.If;
            else if (line.StartsWith("for "))
                node.Type = UniversalNodeType.Loop;
            else if (line.StartsWith("switch "))
                node.Type = UniversalNodeType.Switch;
            else if (line.StartsWith("return"))
                node.Type = UniversalNodeType.Return;
            else if (line.Contains(":=") || (line.Contains("=") && !line.Contains("==")))
                node.Type = UniversalNodeType.Assignment;

            return node;
        }
    }
}