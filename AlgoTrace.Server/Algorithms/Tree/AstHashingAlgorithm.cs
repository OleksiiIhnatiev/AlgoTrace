using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;
using AlgoTrace.Server.Models.DTO.Analysis;
using AlgoTrace.Server.Models.Tree;

namespace AlgoTrace.Server.Algorithms.Tree
{
    public class AstHashingAlgorithm : ITreeAlgorithm
    {
        public string Key => "ast_hashing";

        public double Calculate(UniversalNode treeA, UniversalNode treeB, out object outMatches)
        {
            var hashA = GetTreeHash(treeA);
            var hashB = GetTreeHash(treeB);
            var matches = new List<DetailedMatch>();
            outMatches = matches;

            if (hashA == hashB)
            {
                matches.Add(new DetailedMatch { Type = "Full Structure Match", Severity = "high" });
                return 100;
            }
            return 0;
        }

        private int GetTreeHash(UniversalNode node)
        {
            var hash = new HashCode();
            hash.Add(node.Type);
            foreach (var child in node.Children)
            {
                hash.Add(GetTreeHash(child));
            }
            return hash.ToHashCode();
        }
    }
}
