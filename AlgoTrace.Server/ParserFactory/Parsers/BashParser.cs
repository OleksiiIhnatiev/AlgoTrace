using System.Text.RegularExpressions;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.Tree;

namespace AlgoTrace.Server.ParserFactory.Parsers
{
    public class BashParser : ICodeParser
    {
        public string Language => "bash";

        public UniversalNode Parse(string code)
        {
            var root = new UniversalNode { Type = UniversalNodeType.Program, Value = "BashScript" };
            var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var stack = new Stack<UniversalNode>();
            stack.Push(root);

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;

                if (trimmed == "fi" || trimmed == "done" || trimmed == "esac" || trimmed == "}")
                {
                    if (stack.Count > 1) stack.Pop();
                    continue;
                }

                var node = new UniversalNode { Type = UniversalNodeType.Unknown, Value = "" };

                if (trimmed.StartsWith("function ") || trimmed.Contains("() {"))
                {
                    node.Type = UniversalNodeType.Method;
                    node.Value = Regex.Match(trimmed, @"(?:function\s+)?(\w+)\s*\(").Groups[1].Value;
                }
                else if (trimmed.StartsWith("if ") || trimmed.StartsWith("elif ") || trimmed.StartsWith("else")) node.Type = UniversalNodeType.If;
                else if (trimmed.StartsWith("for ") || trimmed.StartsWith("while ")) node.Type = UniversalNodeType.Loop;
                else if (trimmed.StartsWith("case ")) node.Type = UniversalNodeType.Switch;
                else if (trimmed.Contains("=") && !trimmed.Contains("==") && !trimmed.StartsWith("if")) node.Type = UniversalNodeType.Assignment;

                if (node.Type != UniversalNodeType.Unknown)
                    stack.Peek().Children.Add(node);

                if (trimmed.EndsWith("then") || trimmed.EndsWith("do") || trimmed.StartsWith("case ") || trimmed.EndsWith("{"))
                {
                    var blockNode = node.Type != UniversalNodeType.Unknown ? node : new UniversalNode { Type = "Block" };
                    if (node.Type == UniversalNodeType.Unknown) stack.Peek().Children.Add(blockNode);
                    stack.Push(blockNode);
                }
            }
            return root;
        }
    }
}