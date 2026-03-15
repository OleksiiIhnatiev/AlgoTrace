using AlgoTrace.Server.Models.Tree;

namespace AlgoTrace.Server.Interfaces
{
    public interface ITreeAlgorithm
    {
        string Key { get; }
        double Calculate(UniversalNode treeA, UniversalNode treeB, out object matches);
    }
}
