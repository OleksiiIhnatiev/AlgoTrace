using System.Text.RegularExpressions;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.Tree;

namespace AlgoTrace.Server.ParserFactory.Parsers
{
    public class RubyParser : ICodeParser
    {
        public string Language => "ruby";

        public UniversalNode Parse(string code)
        {
            var root = new UniversalNode { Type = UniversalNodeType.Program, Value = "RubyScript" };
            var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var stack = new Stack<UniversalNode>();
            stack.Push(root);

            var blockOpeners = new[] { "class ", "module ", "def ", "if ", "unless ", "while ", "until ", "for ", "begin " };

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;

                if (trimmed == "end" && stack.Count > 1)
                {
                    stack.Pop();
                    continue;
                }

                var node = IdentifyNode(trimmed);
                if (node.Type != UniversalNodeType.Unknown)
                    stack.Peek().Children.Add(node);

                if (blockOpeners.Any(op => trimmed.StartsWith(op)) || trimmed.EndsWith(" do"))
                {
                    var blockNode = node.Type != UniversalNodeType.Unknown ? node : new UniversalNode { Type = "Block" };
                    if (node.Type == UniversalNodeType.Unknown) stack.Peek().Children.Add(blockNode);
                    stack.Push(blockNode);
                }
            }
            return root;
        }

        private UniversalNode IdentifyNode(string line)
        {
            var node = new UniversalNode { Type = UniversalNodeType.Unknown, Value = "" };

            if (line.StartsWith("class ") || line.StartsWith("module "))
            {
                node.Type = UniversalNodeType.Class;
                node.Value = Regex.Match(line, @"(?:class|module)\s+([\w:]+)").Groups[1].Value;
            }
            else if (line.StartsWith("def "))
            {
                node.Type = UniversalNodeType.Method;
                node.Value = Regex.Match(line, @"def\s+([\w\.\!\?]+)").Groups[1].Value;
            }
            else if (line.StartsWith("if ") || line.StartsWith("unless ") || line.StartsWith("elsif "))
                node.Type = UniversalNodeType.If;
            else if (line.StartsWith("while ") || line.StartsWith("until ") || line.StartsWith("for ") || line.EndsWith(" do"))
                node.Type = UniversalNodeType.Loop;
            else if (line.Contains("=") && !line.Contains("=="))
                node.Type = UniversalNodeType.Assignment;

            return node;
        }
    }
}