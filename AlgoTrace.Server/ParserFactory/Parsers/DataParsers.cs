using AlgoTrace.Server.Models.Tree;
using AlgoTrace.Server.ParserFactory.Parsers.Base;

namespace AlgoTrace.Server.ParserFactory.Parsers
{
    public class JsonParser : BraceLanguageParser
    {
        public override string Language => "json";

        protected override UniversalNode IdentifyNode(string line)
        {
            var node = new UniversalNode { Type = UniversalNodeType.Unknown, Value = "" };

            var match = System.Text.RegularExpressions.Regex.Match(line, @"""([^""]+)""\s*:");
            if (match.Success)
            {
                node.Type = "Property";
                node.Value = match.Groups[1].Value;
            }
            return node;
        }
    }

    public class YamlParser : PythonParser
    {
        public override string Language => "yaml";
    }
}
