using System.Text.RegularExpressions;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.Tree;

namespace AlgoTrace.Server.ParserFactory.Parsers
{
    public class PythonParser : ICodeParser
    {
        public virtual string Language => "python";

        public UniversalNode Parse(string code)
        {
            var root = new UniversalNode { Type = UniversalNodeType.Program, Value = "PythonModule" };
            var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var stack = new Stack<(int indent, UniversalNode node)>();
            stack.Push((-1, root));

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    continue;

                int indent = line.TakeWhile(c => c == ' ' || c == '\t').Count();
                string trimmed = line.Trim();

                while (stack.Count > 1 && stack.Peek().indent >= indent)
                {
                    stack.Pop();
                }

                var node = CreateNodeFromLine(trimmed);
                stack.Peek().node.Children.Add(node);

                if (trimmed.EndsWith(":"))
                {
                    stack.Push((indent, node));
                }
            }

            return root;
        }

        private UniversalNode CreateNodeFromLine(string line)
        {
            var node = new UniversalNode { Type = UniversalNodeType.Unknown, Value = "" };

            if (line.StartsWith("class "))
            {
                node.Type = UniversalNodeType.Class;
                node.Value = Regex.Match(line, @"class\s+(\w+)").Groups[1].Value;
            }
            else if (line.StartsWith("def "))
            {
                node.Type = UniversalNodeType.Method;
                node.Value = Regex.Match(line, @"def\s+(\w+)").Groups[1].Value;
            }
            else if (line.StartsWith("if ") || line.StartsWith("elif ") || line.StartsWith("else:"))
            {
                node.Type = UniversalNodeType.If;
            }
            else if (line.StartsWith("for ") || line.StartsWith("while "))
            {
                node.Type = UniversalNodeType.Loop;
            }
            else if (line.StartsWith("return"))
            {
                node.Type = UniversalNodeType.Return;
            }
            else if (line.StartsWith("try:") || line.StartsWith("except") || line.StartsWith("finally:"))
            {
                node.Type = UniversalNodeType.TryCatch;
            }
            else if (line.Contains("="))
            {
                if (!line.Contains("==") && Regex.IsMatch(line, @"\w+\s*=\s*.+"))
                    node.Type = UniversalNodeType.Assignment;
                else
                    node.Type = UniversalNodeType.BinaryOperation;
            }
            else if (line.Contains("(") && line.Contains(")"))
            {
                node.Type = UniversalNodeType.MethodCall;
                node.Value = Regex.Match(line, @"(\w+)\s*\(").Groups[1].Value;
            }

            return node;
        }
    }
}