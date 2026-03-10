using AlgoTrace.Server.ParserFactory;

namespace AlgoTrace.Server.Interfaces
{
    public interface ICodeParser
    {
        string Language { get; }
        UniversalNode Parse(string code);
    }
}
