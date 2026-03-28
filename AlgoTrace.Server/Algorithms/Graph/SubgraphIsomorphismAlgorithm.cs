using System;
using System.Collections.Generic;
using System.Linq;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO.Analysis;
using AlgoTrace.Server.Models.Tree;
using static AlgoTrace.Server.Algorithms.Graph.GraphUtils;

namespace AlgoTrace.Server.Algorithms.Graph
{
    public class SubgraphIsomorphismAlgorithm : IGraphAlgorithm
    {
        public string Key => "subgraph_isomorphism";
        public string Name => "Subgraph Isomorphism Search";

        public List<DetailedMatch> Execute(
            UniversalNode treeA,
            UniversalNode treeB,
            Dictionary<string, object> parameters,
            out double similarityScore,
            out CodeGraph graphA,
            out CodeGraph graphB
        )
        {
            var localGraphA = GraphUtils.BuildGraph(treeA);
            var localGraphB = GraphUtils.BuildGraph(treeB);

            // Присваиваем out параметрам значения
            graphA = localGraphA;
            graphB = localGraphB;

            var matches = new List<DetailedMatch>();
            similarityScore = 0.0;

            if (localGraphA.Nodes.Count == 0 || localGraphB.Nodes.Count == 0)
            {
                return matches;
            }

            // Предохранитель от бесконечного зависания (задача NP-полная)
            int maxIterations =
                parameters != null && parameters.ContainsKey("max_iterations")
                    ? Convert.ToInt32(parameters["max_iterations"])
                    : 100000;
            int iterations = 0;

            var bestMapping = new Dictionary<int, int>();

            // Оптимизация: быстрый поиск ребер (Adjacency Lists)
            var outEdgesA = BuildAdjacencyList(localGraphA.Edges, true);
            var inEdgesA = BuildAdjacencyList(localGraphA.Edges, false);

            // Хэш-сет для O(1) проверки существования ребра в графе B
            var edgesBSet = new HashSet<string>();
            foreach (var edge in localGraphB.Edges)
            {
                edgesBSet.Add($"{edge.SourceId}_{edge.TargetId}_{edge.Type}");
            }

            // Группировка узлов B по типу для быстрого поиска кандидатов
            var nodesBByType = localGraphB
                .Nodes.GroupBy(n => n.Type)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Эвристика: сортируем узлы A по степени связности (Degree).
            // Узлы с большим количеством связей отсекают неверные ветви раньше (как в алгоритме VF2).
            var sortedNodesA = localGraphA
                .Nodes.OrderByDescending(n =>
                    (outEdgesA.ContainsKey(n.Id) ? outEdgesA[n.Id].Count : 0)
                    + (inEdgesA.ContainsKey(n.Id) ? inEdgesA[n.Id].Count : 0)
                )
                .ToList();

            // Рекурсивный поиск с возвратом (Backtracking)
            void Backtrack(int uIndex, Dictionary<int, int> currentMapping)
            {
                iterations++;
                if (iterations > maxIterations)
                    return;

                if (currentMapping.Count > bestMapping.Count)
                {
                    bestMapping = new Dictionary<int, int>(currentMapping);
                    // Если нашли полное совпадение всех узлов, прерываем поиск
                    if (bestMapping.Count == localGraphA.Nodes.Count)
                        return;
                }

                if (uIndex >= sortedNodesA.Count)
                    return;

                var nodeA = sortedNodesA[uIndex];

                // Ищем возможных кандидатов в B (узлы с таким же типом)
                if (nodesBByType.TryGetValue(nodeA.Type, out var candidatesB))
                {
                    foreach (var nodeB in candidatesB)
                    {
                        if (currentMapping.ContainsValue(nodeB.Id))
                            continue;

                        if (
                            IsFeasible(nodeA, nodeB, currentMapping, outEdgesA, inEdgesA, edgesBSet)
                        )
                        {
                            currentMapping[nodeA.Id] = nodeB.Id;
                            Backtrack(uIndex + 1, currentMapping);
                            currentMapping.Remove(nodeA.Id); // Возврат (backtrack)

                            if (
                                iterations > maxIterations
                                || bestMapping.Count == localGraphA.Nodes.Count
                            )
                                return;
                        }
                    }
                }

                // Также пробуем ветку, где этот узел не замаплен (чтобы найти максимальный ОБЩИЙ подграф, а не только полный)
                Backtrack(uIndex + 1, currentMapping);
            }

            Backtrack(0, new Dictionary<int, int>());

            var nodesAById = localGraphA.Nodes.ToDictionary(n => n.Id);
            var nodesBById = localGraphB.Nodes.ToDictionary(n => n.Id);

            // Формирование результатов
            foreach (var kvp in bestMapping)
            {
                var nA = nodesAById[kvp.Key];
                var nB = nodesBById[kvp.Value];

                matches.Add(
                    new DetailedMatch
                    {
                        Id = nA.Id + 3000,
                        Type = "Full Structure Match",
                        LeftLines = new List<int> { nA.LineIndex + 1, nA.LineIndex + 1 },
                        RightLines = new List<int> { nB.LineIndex + 1, nB.LineIndex + 1 },
                        Severity = "high",
                    }
                );
            }

            if (localGraphA.Nodes.Count > 0)
            {
                similarityScore = Math.Round(
                    ((double)bestMapping.Count / localGraphA.Nodes.Count) * 100.0,
                    2
                );
            }

            return matches;
        }

        private Dictionary<int, List<GraphEdge>> BuildAdjacencyList(
            List<GraphEdge> edges,
            bool isOut
        )
        {
            var dict = new Dictionary<int, List<GraphEdge>>();
            foreach (var edge in edges)
            {
                int key = isOut ? edge.SourceId : edge.TargetId;
                if (!dict.ContainsKey(key))
                    dict[key] = new List<GraphEdge>();
                dict[key].Add(edge);
            }
            return dict;
        }

        private bool IsFeasible(
            GraphNode nodeA,
            GraphNode nodeB,
            Dictionary<int, int> currentMapping,
            Dictionary<int, List<GraphEdge>> outEdgesA,
            Dictionary<int, List<GraphEdge>> inEdgesA,
            HashSet<string> edgesBSet
        )
        {
            // Проверяем исходящие ребра от nodeA к уже замапленным узлам
            if (outEdgesA.TryGetValue(nodeA.Id, out var outs))
            {
                foreach (var edge in outs)
                {
                    if (currentMapping.TryGetValue(edge.TargetId, out int mappedTargetB))
                    {
                        if (!edgesBSet.Contains($"{nodeB.Id}_{mappedTargetB}_{edge.Type}"))
                            return false;
                    }
                }
            }

            // Проверяем входящие ребра от уже замапленных узлов к nodeA
            if (inEdgesA.TryGetValue(nodeA.Id, out var ins))
            {
                foreach (var edge in ins)
                {
                    if (currentMapping.TryGetValue(edge.SourceId, out int mappedSourceB))
                    {
                        if (!edgesBSet.Contains($"{mappedSourceB}_{nodeB.Id}_{edge.Type}"))
                            return false;
                    }
                }
            }

            return true;
        }
    }
}
