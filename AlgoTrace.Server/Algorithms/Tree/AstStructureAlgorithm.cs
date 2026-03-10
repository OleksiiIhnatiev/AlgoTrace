using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;
using AlgoTrace.Server.ParserFactory;

namespace AlgoTrace.Server.Algorithms.Tree
{
    public class AstStructureAlgorithm : ITreeAlgorithm
    {
        public string Key => "ast_compare";

        public double Calculate(UniversalNode treeA, UniversalNode treeB, out List<DetailedMatch> matches)
        {
            var kindsA = treeA.Flatten().Select(n => n.Type).ToList();
            var kindsB = treeB.Flatten().Select(n => n.Type).ToList();

            if (kindsA.Count == 0 || kindsB.Count == 0)
            {
                matches = new List<DetailedMatch>();
                return 0;
            }

            int commonNodes = kindsA.Intersect(kindsB).Count();
            double score = (double)commonNodes / Math.Max(kindsA.Count, kindsB.Count) * 100;

            matches = new List<DetailedMatch>
            {
                new DetailedMatch
                {
                    Type = "Structural AST Similarity",
                    Severity = score > 80 ? "high" : (score > 40 ? "med" : "low")
                }
            };

            return score;
        }
    }
}