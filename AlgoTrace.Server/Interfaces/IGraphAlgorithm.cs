using AlgoTrace.Server.Algorithms.Graph;
using AlgoTrace.Server.Models.DTO.Analysis;
using AlgoTrace.Server.Models.Tree;
using static AlgoTrace.Server.Algorithms.Graph.GraphUtils;

namespace AlgoTrace.Server.Interfaces
{
    public interface IGraphAlgorithm
    {
        string Key { get; }
        string Name { get; }
        List<DetailedMatch> Execute(
            UniversalNode treeA,
            UniversalNode treeB,
            Dictionary<string, object> parameters,
            out double similarityScore,
            out CodeGraph graphA,
            out CodeGraph graphB
        );
    }
}
