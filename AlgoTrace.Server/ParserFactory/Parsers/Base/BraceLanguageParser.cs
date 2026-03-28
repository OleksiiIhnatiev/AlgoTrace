using System.Text.RegularExpressions;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.Tree;

namespace AlgoTrace.Server.ParserFactory.Parsers.Base
{
    public abstract class BraceLanguageParser : ICodeParser
    {
        public abstract string Language { get; }

        public UniversalNode Parse(string code)
        {
            return Parse(code, true);
        }

        public UniversalNode Parse(string code, bool ignoreComments)
        {
            var root = new UniversalNode { Type = UniversalNodeType.Program, Value = Language };

            code = SanitizeCode(code, ignoreComments);
            var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var stack = new Stack<UniversalNode>();
            stack.Push(root);

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("//"))
                    continue;

                if (trimmed.StartsWith("}") && stack.Count > 1)
                {
                    stack.Pop();
                    trimmed = trimmed.Substring(1).Trim();
                    if (string.IsNullOrEmpty(trimmed))
                        continue;
                }

                var node = IdentifyNode(trimmed);

                if (node.Type != UniversalNodeType.Unknown)
                {
                    stack.Peek().Children.Add(node);
                }

                if (trimmed.EndsWith("{") || trimmed.Contains("{"))
                {
                    var blockNode =
                        node.Type != UniversalNodeType.Unknown
                            ? node
                            : new UniversalNode { Type = "Block" };
                    if (node.Type == UniversalNodeType.Unknown)
                        stack.Peek().Children.Add(blockNode);
                    stack.Push(blockNode);
                }
            }

            return root;
        }

        protected virtual string SanitizeCode(string code, bool ignoreComments)
        {
            // Безопасно удаляем комментарии и содержимое строк, не ломая структуру
            var pattern =
                @"(@""(?:[^""]|"""")*""|""(?:\\.|[^\\""])*""|'(?:\\.|[^\\'])*'|`(?:\\.|[^\\`])*`|//.*?$|/\*[\s\S]*?\*/)";
            return Regex.Replace(
                code,
                pattern,
                match =>
                {
                    if (
                        ignoreComments
                        && (match.Value.StartsWith("//") || match.Value.StartsWith("/*"))
                    )
                        return new string('\n', match.Value.Count(c => c == '\n')); // Сохраняем переносы строк
                    if (
                        !ignoreComments
                        && (match.Value.StartsWith("//") || match.Value.StartsWith("/*"))
                    )
                        return match.Value; // Оставляем комментарии, если настройка игнорирования выключена
                    return "\"STR\""; // Заменяем строки на плейсхолдер
                },
                RegexOptions.Multiline
            );
        }

        protected abstract UniversalNode IdentifyNode(string line);
    }
}
