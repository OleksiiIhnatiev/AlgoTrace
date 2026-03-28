using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO;
using AlgoTrace.Server.Models.DTO.Analysis;
using AlgoTrace.Server.Models.Tree;
using AlgoTrace.Server.Utils;

namespace AlgoTrace.Server.Algorithms.Tree
{
    public class AstStructureAlgorithm : ITreeAlgorithm
    {
        public string Key => "ast_compare";

        public double Calculate(
            UniversalNode treeA,
            UniversalNode treeB,
            Dictionary<string, object> parameters,
            out object outMatches
        )
        {
            var evidence = new TreeEvidence();
            outMatches = evidence;

            bool ignoreWhitespace = true;
            if (parameters != null && parameters.TryGetValue("ignore_whitespace", out var wVal))
            {
                if (
                    wVal is JsonElement elem
                    && (
                        elem.ValueKind == JsonValueKind.True
                        || elem.ValueKind == JsonValueKind.False
                    )
                )
                    ignoreWhitespace = elem.GetBoolean();
                else if (wVal is bool b)
                    ignoreWhitespace = b;
            }

            if (treeA == null || treeB == null)
                return 0;

            // 3. Честная математика: Считаем только размер файла А
            int totalNodesA = GetSubtreeSize(treeA);
            if (totalNodesA == 0)
                return 0;

            var matchedNodesB = new HashSet<UniversalNode>();
            int matchedNodesCount = 0;

            // 1. Никакого Flatten: Рекурсивный обход сверху вниз
            void TraverseAndMatch(UniversalNode nodeA)
            {
                var matchB = FindMatchInB(nodeA, treeB, matchedNodesB, ignoreWhitespace);

                if (matchB != null)
                {
                    int sizeA = GetSubtreeSize(nodeA);
                    matchedNodesCount += sizeA;

                    // 4. Защита от двойного счета: помечаем все узлы поддерева B как использованные
                    MarkSubtreeAsMatched(matchB, matchedNodesB);

                    var subtreeNodesA = new List<UniversalNode>();
                    CollectSubtreeNodes(nodeA, subtreeNodesA);

                    var subtreeNodesB = new List<UniversalNode>();
                    CollectSubtreeNodes(matchB, subtreeNodesB);

                    evidence.MatchedSubtrees.Add(
                        new MatchedSubtree
                        {
                            NodeType = nodeA.Type,
                            NodesA = subtreeNodesA
                                .Select<UniversalNode, MatchedNodeInfo>(n => CreateNodeInfo(n, "a"))
                                .ToList(),
                            NodesB = subtreeNodesB
                                .Select<UniversalNode, MatchedNodeInfo>(n => CreateNodeInfo(n, "b"))
                                .ToList(),
                        }
                    );

                    // Важно: мы НЕ идем вглубь nodeA, так как все его поддерево уже найдено целиком!
                }
                else
                {
                    // Если совпадений для крупного поддерева нет, ищем совпадения для его детей
                    foreach (var childA in nodeA.Children)
                    {
                        TraverseAndMatch(childA);
                    }
                }
            }

            TraverseAndMatch(treeA);

            double score = ((double)matchedNodesCount / totalNodesA) * 100.0;
            return Math.Round(Math.Min(100.0, score), 2);
        }

        private UniversalNode FindMatchInB(
            UniversalNode nodeA,
            UniversalNode currentB,
            HashSet<UniversalNode> matchedB,
            bool ignoreWhitespace
        )
        {
            if (currentB == null || matchedB.Contains(currentB))
                return null;

            if (AreNodesStructurallyEqual(nodeA, currentB, matchedB, ignoreWhitespace))
                return currentB;

            foreach (var childB in currentB.Children)
            {
                var match = FindMatchInB(nodeA, childB, matchedB, ignoreWhitespace);
                if (match != null)
                    return match;
            }

            return null;
        }

        private MatchedNodeInfo CreateNodeInfo(UniversalNode node, string submissionPrefix)
        {
            return new MatchedNodeInfo
            {
                Id = $"{submissionPrefix}_{node.GetHashCode()}",
                Label = node.Type,
                Children = node
                    .Children.Select<UniversalNode, string>(c =>
                        $"{submissionPrefix}_{c.GetHashCode()}"
                    )
                    .ToList(),
                Location = node.Location ?? new CodeLocation(), // Handle null location
            };
        }

        private bool AreNodesStructurallyEqual(
            UniversalNode a,
            UniversalNode b,
            HashSet<UniversalNode> matchedB,
            bool ignoreWhitespace
        )
        {
            if (matchedB.Contains(b))
                return false;
            if (a.Type != b.Type || a.Children.Count != b.Children.Count)
                return false;

            // 2. Точное сравнение узлов (контент тоже должен совпадать)
            bool hasValueA = !string.IsNullOrWhiteSpace(a.Value);
            bool hasValueB = !string.IsNullOrWhiteSpace(b.Value);
            if (hasValueA || hasValueB)
            {
                var valA = ignoreWhitespace
                    ? SourceNormalizer.NormalizeLine(a.Value, true)
                    : a.Value;
                var valB = ignoreWhitespace
                    ? SourceNormalizer.NormalizeLine(b.Value, true)
                    : b.Value;
                if (valA != valB)
                    return false;
            }

            for (int i = 0; i < a.Children.Count; i++)
            {
                if (
                    !AreNodesStructurallyEqual(
                        a.Children[i],
                        b.Children[i],
                        matchedB,
                        ignoreWhitespace
                    )
                )
                    return false;
            }
            return true;
        }

        private int GetSubtreeSize(UniversalNode node)
        {
            if (node == null)
                return 0;
            int size = 1;
            foreach (var child in node.Children)
                size += GetSubtreeSize(child);
            return size;
        }

        private void MarkSubtreeAsMatched(UniversalNode node, HashSet<UniversalNode> matchedB)
        {
            if (node == null)
                return;
            matchedB.Add(node);
            foreach (var child in node.Children)
                MarkSubtreeAsMatched(child, matchedB);
        }

        private void CollectSubtreeNodes(UniversalNode node, List<UniversalNode> list)
        {
            if (node == null)
                return;
            list.Add(node);
            foreach (var child in node.Children)
                CollectSubtreeNodes(child, list);
        }
    }
}
