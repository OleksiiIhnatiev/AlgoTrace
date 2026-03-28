using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO.Analysis;
using AlgoTrace.Server.Models.Tree;
using System;
using System.Collections.Generic;
using System.Text.Json;
using AlgoTrace.Server.Utils;
using static AlgoTrace.Server.Algorithms.Graph.GraphUtils;

namespace AlgoTrace.Server.Algorithms.Graph
{
    public class ControlFlowGraphAlgorithm : IGraphAlgorithm
    {
        public string Key => "cfg";
        public string Name => "Control Flow Graph Comparison";

        public List<DetailedMatch> Execute(
            UniversalNode treeA,
            UniversalNode treeB,
            Dictionary<string, object> parameters,
            out double similarityScore,
            out CodeGraph graphA,
            out CodeGraph graphB
        )
        {
            graphA = GraphUtils.BuildGraph(treeA);
            graphB = GraphUtils.BuildGraph(treeB);

            var matches = new List<DetailedMatch>();
            similarityScore = 0;

            bool ignoreWhitespace = true;
            if (parameters != null && parameters.TryGetValue("ignore_whitespace", out var wVal))
            {
                if (wVal is JsonElement elem && (elem.ValueKind == JsonValueKind.True || elem.ValueKind == JsonValueKind.False))
                    ignoreWhitespace = elem.GetBoolean();
                else if (wVal is bool b) ignoreWhitespace = b;
            }

            if (graphA.Nodes.Count == 0 || graphB.Nodes.Count == 0) return matches;

            int countA = graphA.Nodes.Count;
            int countB = graphB.Nodes.Count;

            // ==========================================
            // ШАГ 1: Поиск базового маппинга узлов (Vertices)
            // ==========================================
            int[,] dp = new int[countA + 1, countB + 1];

            for (int i = 1; i <= countA; i++)
            {
                for (int j = 1; j <= countB; j++)
                {
                    var nodeA = graphA.Nodes[i - 1];
                    var nodeB = graphB.Nodes[j - 1];

                    var contentA = ignoreWhitespace ? SourceNormalizer.NormalizeLine(nodeA.Content, true) : nodeA.Content;
                    var contentB = ignoreWhitespace ? SourceNormalizer.NormalizeLine(nodeB.Content, true) : nodeB.Content;

                    if (nodeA.Type == nodeB.Type && contentA == contentB)
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                    }
                }
            }

            // Восстанавливаем карту соответствий (какой NodeId из A перешел в какой NodeId из B)
            var mappedNodesAtoB = new Dictionary<int, int>();

            int x = countA;
            int y = countB;

            while (x > 0 && y > 0)
            {
                var nodeA = graphA.Nodes[x - 1];
                var nodeB = graphB.Nodes[y - 1];

                var contentA = ignoreWhitespace ? SourceNormalizer.NormalizeLine(nodeA.Content, true) : nodeA.Content;
                var contentB = ignoreWhitespace ? SourceNormalizer.NormalizeLine(nodeB.Content, true) : nodeB.Content;

                if (nodeA.Type == nodeB.Type && contentA == contentB)
                {
                    // Сохраняем маппинг для проверки ребер
                    mappedNodesAtoB[nodeA.Id] = nodeB.Id;

                    matches.Add(new DetailedMatch
                    {
                        Id = nodeA.Id + 1000,
                        Type = "CFG Node Match",
                        LeftLines = new List<int> { nodeA.LineIndex + 1, nodeA.LineIndex + 1 },
                        RightLines = new List<int> { nodeB.LineIndex + 1, nodeB.LineIndex + 1 },
                        Severity = "med",
                    });
                    x--;
                    y--;
                }
                else if (dp[x - 1, y] > dp[x, y - 1])
                {
                    x--;
                }
                else
                {
                    y--;
                }
            }

            matches.Reverse();
            int matchedNodesCount = mappedNodesAtoB.Count;


            // ==========================================
            // ШАГ 2: Проверка топологии графа (Edges)
            // ==========================================
            int matchedEdgesCount = 0;

            // Создаем Hash-таблицу связей файла B для O(1) поиска
            var edgesSetB = new HashSet<string>();
            foreach (var edgeB in graphB.Edges)
            {
                // Уникальная сигнатура ребра: Откуда_Куда_Тип
                edgesSetB.Add($"{edgeB.SourceId}_{edgeB.TargetId}_{edgeB.Type}");
            }

            // Проверяем каждое ребро из главного файла А
            foreach (var edgeA in graphA.Edges)
            {
                // Если оба узла этого ребра имеют совпадения в файле B...
                if (mappedNodesAtoB.TryGetValue(edgeA.SourceId, out int mappedSourceB) &&
                    mappedNodesAtoB.TryGetValue(edgeA.TargetId, out int mappedTargetB))
                {
                    // Проверяем, существует ли в файле B точно такая же связь между этими узлами
                    string expectedEdgeSignatureB = $"{mappedSourceB}_{mappedTargetB}_{edgeA.Type}";

                    if (edgesSetB.Contains(expectedEdgeSignatureB))
                    {
                        matchedEdgesCount++;
                    }
                }
            }


            // ==========================================
            // ШАГ 3: Итоговый расчет 100% честного CFG
            // ==========================================
            double totalElementsA = graphA.Nodes.Count + graphA.Edges.Count;
            double totalMatchedElements = matchedNodesCount + matchedEdgesCount;

            if (totalElementsA > 0)
            {
                similarityScore = (totalMatchedElements / totalElementsA) * 100.0;
            }

            similarityScore = Math.Round(similarityScore, 2);

            return matches;
        }
    }
}