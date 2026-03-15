using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO.Analysis;
using AlgoTrace.Server.Models.Tree;

namespace AlgoTrace.Server.Algorithms.Graph
{
    public class ProgramDependenceGraphAlgorithm : IGraphAlgorithm
    {
        public string Key => "pdg";
        public string Name => "Program Dependence Graph";

        public List<DetailedMatch> Execute(
            UniversalNode treeA,
            UniversalNode treeB,
            Dictionary<string, object> parameters,
            out double similarityScore,
            out CodeGraph graphA,
            out CodeGraph graphB
        )
        {
            graphA = GraphUtils.BuildGraph(treeA, includeDataDeps: true);
            graphB = GraphUtils.BuildGraph(treeB, includeDataDeps: true);

            var matches = new List<DetailedMatch>();
            int edgeMatches = 0;

            var dataEdgesA = graphA.Edges.Where(e => e.Type == "data").ToList();
            var dataEdgesB = graphB.Edges.Where(e => e.Type == "data").ToList();

            foreach (var edgeA in dataEdgesA)
            {
                var srcA = graphA.Nodes.FirstOrDefault(n => n.Id == edgeA.SourceId);
                var tgtA = graphA.Nodes.FirstOrDefault(n => n.Id == edgeA.TargetId);

                foreach (var edgeB in dataEdgesB)
                {
                    var srcB = graphB.Nodes.FirstOrDefault(n => n.Id == edgeB.SourceId);
                    var tgtB = graphB.Nodes.FirstOrDefault(n => n.Id == edgeB.TargetId);

                    if (srcA != null && tgtA != null && srcB != null && tgtB != null)
                    {
                        if (srcA.Type == srcB.Type && tgtA.Type == tgtB.Type)
                        {
                            edgeMatches++;
                            matches.Add(new DetailedMatch
                            {
                                Id = edgeMatches + 5000,
                                Type = "Data Dependency Match",
                                LeftLines = new List<int>
                                {
                                    srcA.LineIndex + 1,
                                    tgtA.LineIndex + 1,
                                },
                                RightLines = new List<int>
                                {
                                    srcB.LineIndex + 1,
                                    tgtB.LineIndex + 1,
                                },
                                Severity = "high",
                            });

                            break;
                        }
                    }
                }
            }

            similarityScore = dataEdgesA.Count > 0
                ? Math.Min(100, (double)edgeMatches / dataEdgesA.Count * 100)
                : 0;

            return matches;
        }
    }
}