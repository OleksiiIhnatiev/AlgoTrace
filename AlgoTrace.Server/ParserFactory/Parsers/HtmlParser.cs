using System.Text.RegularExpressions;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.Tree;

namespace AlgoTrace.Server.ParserFactory.Parsers
{
    public class HtmlParser : ICodeParser
    {
        public virtual string Language => "html";

        public UniversalNode Parse(string code)
        {
            var root = new UniversalNode { Type = "Document", Value = "HTML" };
            var tagRegex = new Regex(@"<(/?)(\w+)([^>]*)>", RegexOptions.Compiled);
            var matches = tagRegex.Matches(code);

            var stack = new Stack<UniversalNode>();
            stack.Push(root);

            var selfClosingTags = new HashSet<string> { "img", "br", "hr", "input", "meta", "link" };

            foreach (Match match in matches)
            {
                bool isClosing = match.Groups[1].Value == "/";
                string tagName = match.Groups[2].Value.ToLower();

                if (isClosing)
                {
                    if (stack.Count > 1 && stack.Peek().Type == tagName)
                        stack.Pop();
                }
                else
                {
                    var node = new UniversalNode { Type = tagName, Value = tagName };
                    stack.Peek().Children.Add(node);

                    if (!selfClosingTags.Contains(tagName) && !match.Value.EndsWith("/>"))
                        stack.Push(node);
                }
            }
            return root;
        }
    }
}