using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO.Analysis;
using AlgoTrace.Server.Models.Tree;

namespace AlgoTrace.Server.Algorithms.Graph
{
    public class SubgraphIsomorphismAlgorithm : IGraphAlgorithm
    {
        public string Key => "subgraph_isomorphism";
        public string Name => "Subgraph Isomorphism Search";

        public List<DetailedMatch> Execute(
            UniversalNode treeA,
            UniversalNode treeB,
            Dictionary<string, object> parameters,
            out double similarityScore,
            out CodeGraph graphA,
            out CodeGraph graphB
        )
        {
            graphA = GraphUtils.BuildGraph(treeA);
            graphB = GraphUtils.BuildGraph(treeB);

            var matches = new List<DetailedMatch>();

            var typesA = graphA.Nodes.Select(n => n.Type).ToList();
            var typesB = graphB.Nodes.Select(n => n.Type).ToList();

            if (typesA.Count == 0 || typesB.Count == 0)
            {
                similarityScore = 0.0;
                return matches;
            }

            for (int i = 0; i <= typesB.Count - typesA.Count; i++)
            {
                bool isSubGraph = true;
                for (int j = 0; j < typesA.Count; j++)
                {
                    if (typesB[i + j] != typesA[j])
                    {
                        isSubGraph = false;
                        break;
                    }
                }

                if (isSubGraph)
                {
                    similarityScore = 100.0;
                    return matches;
                }
            }

            similarityScore = 0.0;
            return matches;
        }
    }
}