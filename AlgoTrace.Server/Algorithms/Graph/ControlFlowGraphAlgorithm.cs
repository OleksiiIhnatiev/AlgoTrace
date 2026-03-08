using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;

namespace AlgoTrace.Server.Algorithms.Graph
{
    public class ControlFlowGraphAlgorithm : IGraphAlgorithm
    {
        public string Key => "cfg";
        public string Name => "Control Flow Graph Comparison";

        public List<DetailedMatch> Execute(
            string sourceCode,
            string targetCode,
            Dictionary<string, object> parameters,
            out double similarityScore
        )
        {
            var graphA = GraphUtils.BuildGraph(sourceCode);
            var graphB = GraphUtils.BuildGraph(targetCode);

            var matches = new List<DetailedMatch>();
            int matchCount = 0;

            int minLen = Math.Min(graphA.Nodes.Count, graphB.Nodes.Count);

            for (int i = 0; i < minLen; i++)
            {
                var nodeA = graphA.Nodes[i];
                var nodeB = graphB.Nodes[i];

                if (nodeA.Type == nodeB.Type)
                {
                    matchCount++;
                    if (nodeA.Content == nodeB.Content)
                    {
                        matches.Add(
                            new DetailedMatch
                            {
                                Id = i + 1000,
                                Type = "CFG Node Match",
                                LeftLines = new List<int>
                                {
                                    nodeA.LineIndex + 1,
                                    nodeA.LineIndex + 1,
                                },
                                RightLines = new List<int>
                                {
                                    nodeB.LineIndex + 1,
                                    nodeB.LineIndex + 1,
                                },
                                Severity = "med",
                            }
                        );
                    }
                }
            }

            similarityScore =
                minLen > 0
                    ? (double)matchCount / Math.Max(graphA.Nodes.Count, graphB.Nodes.Count) * 100
                    : 0;
            return matches;
        }
    }
}
