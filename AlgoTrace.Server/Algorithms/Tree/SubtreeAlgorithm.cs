using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;
using AlgoTrace.Server.ParserFactory;

namespace AlgoTrace.Server.Algorithms.Tree
{
    public class SubtreeAlgorithm : ITreeAlgorithm
    {
        public string Key => "subtree_isomorphism";

        public double Calculate(UniversalNode treeA, UniversalNode treeB, out List<DetailedMatch> matches)
        {
            matches = new List<DetailedMatch>();

            var subtreesA = treeA.Flatten().Where(n => n.Type.Contains("Method") || n.Type.Contains("Function")).ToList();
            var subtreesB = treeB.Flatten().Where(n => n.Type.Contains("Method") || n.Type.Contains("Function")).ToList();

            if (!subtreesA.Any()) return 0;

            int matchedCount = 0;
            foreach (var nodeA in subtreesA)
            {
                foreach (var nodeB in subtreesB)
                {
                    if (AreNodesStructurallyEqual(nodeA, nodeB))
                    {
                        matchedCount++;
                        matches.Add(new DetailedMatch
                        {
                            Type = "Identical Subtree Found",
                            Severity = "high",
                            Id = nodeA.Value.GetHashCode()
                        });
                        break;
                    }
                }
            }

            return (double)matchedCount / subtreesA.Count * 100;
        }

        private bool AreNodesStructurallyEqual(UniversalNode a, UniversalNode b)
        {
            if (a.Type != b.Type || a.Children.Count != b.Children.Count) return false;

            for (int i = 0; i < a.Children.Count; i++)
            {
                if (!AreNodesStructurallyEqual(a.Children[i], b.Children[i])) return false;
            }
            return true;
        }
    }
}