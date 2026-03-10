using AlgoTrace.Server.Models.DTO;
using AlgoTrace.Server.ParserFactory;

namespace AlgoTrace.Server.Interfaces
{
    public interface ITreeAlgorithm
    {
        string Key { get; }
        double Calculate(UniversalNode treeA, UniversalNode treeB, out List<DetailedMatch> matches);
    }
}
