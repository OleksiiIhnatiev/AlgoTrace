using System;
using System.Collections.Generic;
using System.Linq;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO.Analysis;
using AlgoTrace.Server.Models.Tree;
using AlgoTrace.Server.Utils;
using System.Text.Json;

namespace AlgoTrace.Server.Algorithms.Tree
{
    public class AstHashingAlgorithm : ITreeAlgorithm
    {
        public string Key => "ast_hashing";

        public double Calculate(UniversalNode treeA, UniversalNode treeB, Dictionary<string, object> parameters, out object outMatches)
        {
            bool ignoreWhitespace = true;
            if (parameters != null && parameters.TryGetValue("ignore_whitespace", out var wVal))
            {
                if (wVal is JsonElement elem && (elem.ValueKind == JsonValueKind.True || elem.ValueKind == JsonValueKind.False))
                    ignoreWhitespace = elem.GetBoolean();
                else if (wVal is bool b) ignoreWhitespace = b;
            }

            // 3. Сохранять хеши только тех поддеревьев, размер которых >= 3 узлов.
            const int minSubtreeSize = 3;

            // 4. Сбор данных для дерева А
            var hashesA = new Dictionary<int, UniversalNode>();
            ProcessNode(treeA, ignoreWhitespace, (hash, node, size) =>
            {
                if (size >= minSubtreeSize)
                {
                    hashesA.TryAdd(hash, node);
                }
            });

            // 4. Сбор данных для дерева B
            var hashesB = new HashSet<int>();
            ProcessNode(treeB, ignoreWhitespace, (hash, node, size) =>
            {
                if (size >= minSubtreeSize)
                {
                    hashesB.Add(hash);
                }
            });

            // 5. Математика совпадений
            int matchCount = 0;
            var matches = new List<DetailedMatch>();

            foreach (var kvp in hashesA)
            {
                if (hashesB.Contains(kvp.Key))
                {
                    matchCount++;
                    var matchedNodeA = kvp.Value;
                    matches.Add(new DetailedMatch
                    {
                        Type = "Identical Subtree Found",
                        Severity = "high",
                        LeftLines = GetLineRange(matchedNodeA),
                        RightLines = new List<int>() // Невозможно определить соответствующие строки в B
                    });
                }
            }

            outMatches = matches;

            // 6. Вычисление процента
            double similarityScore = 0;
            if (hashesA.Count > 0)
            {
                similarityScore = ((double)matchCount / hashesA.Count) * 100.0;
            }

            return Math.Round(similarityScore, 2);
        }

        // 1. Рекурсивный обход
        private (int hash, int size) ProcessNode(UniversalNode node, bool ignoreWhitespace, Action<int, UniversalNode, int> onHashCalculated)
        {
            var hash = new HashCode();
            // 2. Точность контента
            hash.Add(node.Type);
            if (!string.IsNullOrWhiteSpace(node.Value))
            {
                hash.Add(ignoreWhitespace ? SourceNormalizer.NormalizeLine(node.Value, true) : node.Value);
            }

            int subtreeSize = 1; // Считаем текущий узел

            foreach (var child in node.Children)
            {
                var (childHash, childSize) = ProcessNode(child, ignoreWhitespace, onHashCalculated);
                hash.Add(childHash);
                subtreeSize += childSize;
            }

            int finalHash = hash.ToHashCode();
            onHashCalculated(finalHash, node, subtreeSize);
            return (finalHash, subtreeSize);
        }

        private List<int> GetLineRange(UniversalNode node)
        {
            if (node == null) return new List<int>();

            var allNodes = node.Flatten();
            int minLine = int.MaxValue;
            int maxLine = int.MinValue;
            bool foundLines = false;

            foreach (var n in allNodes)
            {
                if (n.Location != null && n.Location.StartLine > 0)
                {
                    minLine = Math.Min(minLine, n.Location.StartLine);
                    maxLine = Math.Max(maxLine, n.Location.EndLine > 0 ? n.Location.EndLine : n.Location.StartLine);
                    foundLines = true;
                }
            }

            return foundLines ? new List<int> { minLine, maxLine } : new List<int>();
        }
    }
}
