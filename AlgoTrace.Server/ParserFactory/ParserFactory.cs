using AlgoTrace.Server.Interfaces;

namespace AlgoTrace.Server.ParserFactory
{
    public class ParserFactory
    {
        private readonly IEnumerable<ICodeParser> _parsers;

        public ParserFactory(IEnumerable<ICodeParser> parsers)
        {
            _parsers = parsers;
        }

        public ICodeParser GetParser(string language)
        {
            var parser = _parsers.FirstOrDefault(p =>
                p.Language.Equals(language, StringComparison.OrdinalIgnoreCase)
            );
            if (parser == null)
                throw new NotSupportedException($"Language {language} is not supported yet.");
            return parser;
        }
    }
}
