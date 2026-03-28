using System.Text.RegularExpressions;
using AlgoTrace.Server.Models.Tree;
using AlgoTrace.Server.ParserFactory.Parsers.Base;

namespace AlgoTrace.Server.ParserFactory.Parsers
{
    public class JavaParser : BraceLanguageParser
    {
        public override string Language => "java";

        protected override UniversalNode IdentifyNode(string line)
        {
            var node = new UniversalNode { Type = UniversalNodeType.Unknown, Value = "" };

            if (line.Contains("class ") || line.Contains("interface "))
            {
                node.Type = UniversalNodeType.Class;
                node.Value = Regex.Match(line, @"(?:class|interface)\s+(\w+)").Groups[1].Value;
            }
            else if (
                Regex.IsMatch(
                    line,
                    @"(?:public|private|protected|static)\s+[\w<>,\[\]]+\s+(\w+)\s*\("
                )
            )
            {
                node.Type = UniversalNodeType.Method;
                node.Value = Regex.Match(line, @"\s+(\w+)\s*\(").Groups[1].Value;
            }
            else if (line.StartsWith("if") || line.StartsWith("else"))
                node.Type = UniversalNodeType.If;
            else if (line.StartsWith("for") || line.StartsWith("while") || line.StartsWith("do "))
                node.Type = UniversalNodeType.Loop;
            else if (line.StartsWith("switch"))
                node.Type = UniversalNodeType.Switch;
            else if (line.StartsWith("return "))
                node.Type = UniversalNodeType.Return;
            else if (
                line.StartsWith("try")
                || line.StartsWith("catch")
                || line.StartsWith("finally")
            )
                node.Type = UniversalNodeType.TryCatch;
            else if (line.Contains("=") && !line.Contains("=="))
                node.Type = UniversalNodeType.Assignment;
            else if (Regex.IsMatch(line, @"\w+\.\w+\(.*\)"))
                node.Type = UniversalNodeType.MethodCall;

            return node;
        }
    }
}
