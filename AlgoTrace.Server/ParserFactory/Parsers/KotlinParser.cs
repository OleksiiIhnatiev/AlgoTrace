using System.Text.RegularExpressions;
using AlgoTrace.Server.Models.Tree;
using AlgoTrace.Server.ParserFactory.Parsers.Base;

namespace AlgoTrace.Server.ParserFactory.Parsers
{
    public class KotlinParser : BraceLanguageParser
    {
        public override string Language => "kotlin";

        protected override UniversalNode IdentifyNode(string line)
        {
            var node = new UniversalNode { Type = UniversalNodeType.Unknown, Value = "" };

            if (line.StartsWith("class ") || line.StartsWith("interface ") || line.StartsWith("object "))
            {
                node.Type = UniversalNodeType.Class;
                node.Value = Regex.Match(line, @"(?:class|interface|object)\s+(\w+)").Groups[1].Value;
            }
            else if (line.StartsWith("fun "))
            {
                node.Type = UniversalNodeType.Method;
                node.Value = Regex.Match(line, @"fun\s+(?:[\w<>]+\.)?(\w+)\s*\(").Groups[1].Value;
            }
            else if (line.StartsWith("if") || line.StartsWith("else")) node.Type = UniversalNodeType.If;
            else if (line.StartsWith("for") || line.StartsWith("while")) node.Type = UniversalNodeType.Loop;
            else if (line.StartsWith("when")) node.Type = UniversalNodeType.Switch;
            else if (line.StartsWith("return ")) node.Type = UniversalNodeType.Return;
            else if (line.StartsWith("try") || line.StartsWith("catch") || line.StartsWith("finally")) node.Type = UniversalNodeType.TryCatch;
            else if ((line.StartsWith("val ") || line.StartsWith("var ")) && line.Contains("=")) node.Type = UniversalNodeType.VariableDecl;
            else if (line.Contains("=") && !line.Contains("==")) node.Type = UniversalNodeType.Assignment;

            return node;
        }
    }
}