using System.Collections.Generic;
using System.Linq;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;
using AlgoTrace.Server.Models.DTO.Analysis;
using AlgoTrace.Server.Models.Tree;
using AlgoTrace.Server.Utils;
using System.Text.Json;

namespace AlgoTrace.Server.Algorithms.Tree
{
    public class SubtreeAlgorithm : ITreeAlgorithm
    {
        public string Key => "subtree_isomorphism";

        public double Calculate(UniversalNode treeA, UniversalNode treeB, Dictionary<string, object> parameters, out object outMatches)
        {
            var matches = new List<DetailedMatch>();
            outMatches = matches;

            bool ignoreWhitespace = true;
            if (parameters != null && parameters.TryGetValue("ignore_whitespace", out var wVal))
            {
                if (wVal is JsonElement elem && (elem.ValueKind == JsonValueKind.True || elem.ValueKind == JsonValueKind.False))
                    ignoreWhitespace = elem.GetBoolean();
                else if (wVal is bool b) ignoreWhitespace = b;
            }

            var subtreesA = treeA
                .Flatten()
                .Where(IsSignificantSubtree)
                .ToList();
            var subtreesB = treeB
                .Flatten()
                .Where(IsSignificantSubtree)
                .ToList();

            if (!subtreesA.Any())
                return 0;

            int matchedCount = 0;
            var matchedNodesB = new HashSet<UniversalNode>();

            foreach (var nodeA in subtreesA)
            {
                foreach (var nodeB in subtreesB)
                {
                    if (matchedNodesB.Contains(nodeB))
                        continue;

                    if (AreNodesStructurallyEqual(nodeA, nodeB, ignoreWhitespace))
                    {
                        matchedCount++;
                        matchedNodesB.Add(nodeB);

                        matches.Add(
                            new DetailedMatch
                            {
                                Type = "Identical Subtree Found",
                                Severity = "high",
                                Id = nodeA.GetHashCode(),
                            }
                        );
                        break;
                    }
                }
            }

            return (double)matchedCount / subtreesA.Count * 100;
        }

        private bool AreNodesStructurallyEqual(UniversalNode a, UniversalNode b, bool ignoreWhitespace)
        {
            if (a.Type != b.Type || a.Children.Count != b.Children.Count)
                return false;

            // 1. Проверка контента узлов (Content Blindness)
            bool hasValueA = !string.IsNullOrWhiteSpace(a.Value);
            bool hasValueB = !string.IsNullOrWhiteSpace(b.Value);

            if (hasValueA || hasValueB)
            {
                var valA = ignoreWhitespace ? SourceNormalizer.NormalizeLine(a.Value, true) : a.Value;
                var valB = ignoreWhitespace ? SourceNormalizer.NormalizeLine(b.Value, true) : b.Value;
                if (valA != valB)
                    return false;
            }

            for (int i = 0; i < a.Children.Count; i++)
            {
                if (!AreNodesStructurallyEqual(a.Children[i], b.Children[i], ignoreWhitespace))
                    return false;
            }
            return true;
        }

        // 3. Расширенный фильтр логических блоков
        private bool IsSignificantSubtree(UniversalNode node)
        {
            if (node.Children.Count == 0) return false;

            return node.Type == UniversalNodeType.Program || node.Type == UniversalNodeType.Class ||
                   node.Type == UniversalNodeType.Method || node.Type == UniversalNodeType.If ||
                   node.Type == UniversalNodeType.Loop || node.Type == UniversalNodeType.Switch ||
                   node.Type == UniversalNodeType.TryCatch || node.Type.Contains("Block") ||
                   node.Type.Contains("Function");
        }
    }
}
