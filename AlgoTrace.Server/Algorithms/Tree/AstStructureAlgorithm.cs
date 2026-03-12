using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;
using AlgoTrace.Server.ParserFactory;

namespace AlgoTrace.Server.Algorithms.Tree
{
    public class AstStructureAlgorithm : ITreeAlgorithm
    {
        public string Key => "ast_compare";

        public double Calculate(UniversalNode treeA, UniversalNode treeB, out object outMatches)
        {
            var evidence = new TreeEvidence();
            outMatches = evidence;

            var allNodesA = treeA.Flatten().ToList();
            var allNodesB = treeB.Flatten().ToList();

            if (!allNodesA.Any() || !allNodesB.Any())
                return 0;

            var matchedNodesB = new HashSet<UniversalNode>();
            double totalNodes = Math.Max(allNodesA.Count(), allNodesB.Count());
            double matchedNodesCount = 0;

            foreach (var nodeA in allNodesA)
            {
                // Skip trivial nodes for matching, but they count towards total
                if (nodeA.Children.Count == 0 && string.IsNullOrWhiteSpace(nodeA.Value))
                    continue;

                foreach (var nodeB in allNodesB)
                {
                    if (matchedNodesB.Contains(nodeB))
                        continue;

                    if (AreNodesStructurallyEqual(nodeA, nodeB))
                    {
                        var subtreeNodesA = nodeA.Flatten().ToList();
                        var subtreeNodesB = nodeB.Flatten().ToList();

                        // Add evidence for this match
                        evidence.MatchedSubtrees.Add(
                            new MatchedSubtree
                            {
                                NodeType = nodeA.Type,
                                NodesA = new List<MatchedNodeInfo> { CreateNodeInfo(nodeA, "a") },
                                NodesB = new List<MatchedNodeInfo> { CreateNodeInfo(nodeB, "b") },
                            }
                        );

                        matchedNodesCount += subtreeNodesA.Count;
                        foreach (var n in subtreeNodesB)
                            matchedNodesB.Add(n);

                        // Once a node in B is matched, don't match it again
                        break;
                    }
                }
            }

            double score = totalNodes > 0 ? (matchedNodesCount / totalNodes) * 100 : 0;
            return Math.Min(100, score);
        }

        private MatchedNodeInfo CreateNodeInfo(UniversalNode node, string submissionPrefix)
        {
            return new MatchedNodeInfo
            {
                Id = $"{submissionPrefix}_{node.GetHashCode()}",
                Label = node.Type,
                Children = node
                    .Children.Select(c => $"{submissionPrefix}_{c.GetHashCode()}")
                    .ToList(),
                Location = node.Location ?? new CodeLocation(), // Handle null location
            };
        }

        private bool AreNodesStructurallyEqual(UniversalNode a, UniversalNode b)
        {
            if (a.Type != b.Type || a.Children.Count != b.Children.Count)
                return false;

            for (int i = 0; i < a.Children.Count; i++)
            {
                if (!AreNodesStructurallyEqual(a.Children[i], b.Children[i]))
                    return false;
            }
            return true;
        }
    }
}
