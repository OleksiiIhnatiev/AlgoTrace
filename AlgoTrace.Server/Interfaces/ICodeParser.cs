using AlgoTrace.Server.Models.Tree;

namespace AlgoTrace.Server.Interfaces
{
    public interface ICodeParser
    {
        string Language { get; }
        UniversalNode Parse(string code);
    }
}
