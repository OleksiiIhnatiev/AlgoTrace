using System.Text.RegularExpressions;
using AlgoTrace.Server.Models.Tree;
using AlgoTrace.Server.ParserFactory.Parsers.Base;

namespace AlgoTrace.Server.ParserFactory.Parsers
{
    public class PhpParser : BraceLanguageParser
    {
        public override string Language => "php";

        protected override UniversalNode IdentifyNode(string line)
        {
            var node = new UniversalNode { Type = UniversalNodeType.Unknown, Value = "" };

            if (line.Contains("<?php")) return node;

            if (line.Contains("class ") || line.Contains("interface ") || line.Contains("trait "))
            {
                node.Type = UniversalNodeType.Class;
                node.Value = Regex.Match(line, @"(?:class|interface|trait)\s+(\w+)").Groups[1].Value;
            }
            else if (Regex.IsMatch(line, @"function\s+(\w+)\s*\("))
            {
                node.Type = UniversalNodeType.Method;
                node.Value = Regex.Match(line, @"function\s+(\w+)").Groups[1].Value;
            }
            else if (line.StartsWith("if") || line.StartsWith("else") || line.StartsWith("elseif"))
                node.Type = UniversalNodeType.If;
            else if (line.StartsWith("for") || line.StartsWith("while") || line.StartsWith("foreach") || line.StartsWith("do "))
                node.Type = UniversalNodeType.Loop;
            else if (line.StartsWith("return "))
                node.Type = UniversalNodeType.Return;
            else if (line.StartsWith("try") || line.StartsWith("catch") || line.StartsWith("finally"))
                node.Type = UniversalNodeType.TryCatch;
            else if (line.Contains("$") && line.Contains("=") && !line.Contains("=="))
            {
                node.Type = UniversalNodeType.Assignment;
                node.Value = Regex.Match(line, @"\$(\w+)").Groups[1].Value;
            }

            return node;
        }
    }
}