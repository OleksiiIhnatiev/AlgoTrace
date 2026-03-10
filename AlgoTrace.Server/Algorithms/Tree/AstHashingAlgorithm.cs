using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;
using AlgoTrace.Server.ParserFactory;

namespace AlgoTrace.Server.Algorithms.Tree
{
    public class AstHashingAlgorithm : ITreeAlgorithm
    {
        public string Key => "ast_hashing";
        public double Calculate(UniversalNode treeA, UniversalNode treeB, out List<DetailedMatch> matches)
        {
            var hashA = GetTreeHash(treeA);
            var hashB = GetTreeHash(treeB);

            matches = new List<DetailedMatch>();
            if (hashA == hashB)
            {
                matches.Add(new DetailedMatch { Type = "Full Structure Match", Severity = "high" });
                return 100;
            }
            return 0;
        }
        private int GetTreeHash(UniversalNode node) =>
            HashCode.Combine(node.Type, node.Children.Sum(c => GetTreeHash(c)));
    }
}