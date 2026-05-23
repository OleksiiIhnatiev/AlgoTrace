using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.DTO.Analysis;
using AlgoTrace.Server.Models.Tree;
using AlgoTrace.Server.Utils;
using static AlgoTrace.Server.Algorithms.Graph.GraphUtils;

namespace AlgoTrace.Server.Algorithms.Graph
{
    public class ProgramDependenceGraphAlgorithm : IGraphAlgorithm
    {
        public string Key => "pdg";
        public string Name => "Program Dependence Graph";

        public List<DetailedMatch> Execute(
            UniversalNode treeA,
            UniversalNode treeB,
            Dictionary<string, object> parameters,
            out double similarityScore,
            out CodeGraph graphA,
            out CodeGraph graphB
        )
        {
            graphA = GraphUtils.BuildGraph(treeA, includeDataDeps: true);
            graphB = GraphUtils.BuildGraph(treeB, includeDataDeps: true);

            var matches = new List<DetailedMatch>();
            int edgeMatches = 0;

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

            var dataEdgesA = graphA.Edges.Where(e => e.Type == "data").ToList();
            var dataEdgesB = graphB.Edges.Where(e => e.Type == "data").ToList();

            similarityScore = 0;
            if (dataEdgesA.Count == 0)
                return matches;

            var nodesA = graphA.Nodes.ToDictionary(n => n.Id);
            var nodesB = graphB.Nodes.ToDictionary(n => n.Id);

            var usedEdgesB = new HashSet<int>();

            foreach (var edgeA in dataEdgesA)
            {
                if (
                    !nodesA.TryGetValue(edgeA.SourceId, out var srcA)
                    || !nodesA.TryGetValue(edgeA.TargetId, out var tgtA)
                )
                    continue;

                for (int i = 0; i < dataEdgesB.Count; i++)
                {
                    if (usedEdgesB.Contains(i))
                        continue; 

                    var edgeB = dataEdgesB[i];

                    if (
                        !nodesB.TryGetValue(edgeB.SourceId, out var srcB)
                        || !nodesB.TryGetValue(edgeB.TargetId, out var tgtB)
                    )
                        continue;

                    var srcContentA = ignoreWhitespace
                        ? SourceNormalizer.NormalizeLine(srcA.Content, true)
                        : srcA.Content;
                    var srcContentB = ignoreWhitespace
                        ? SourceNormalizer.NormalizeLine(srcB.Content, true)
                        : srcB.Content;
                    var tgtContentA = ignoreWhitespace
                        ? SourceNormalizer.NormalizeLine(tgtA.Content, true)
                        : tgtA.Content;
                    var tgtContentB = ignoreWhitespace
                        ? SourceNormalizer.NormalizeLine(tgtB.Content, true)
                        : tgtB.Content;

                    if (
                        srcA.Type == srcB.Type
                        && srcContentA == srcContentB
                        && tgtA.Type == tgtB.Type
                        && tgtContentA == tgtContentB
                    )
                    {
                        edgeMatches++;
                        usedEdgesB.Add(i); 

                        matches.Add(
                            new DetailedMatch
                            {
                                Id = edgeMatches + 5000,
                                Type = "Data Dependency Match",
                                LeftLines = new List<int>
                                {
                                    srcA.LineIndex + 1,
                                    tgtA.LineIndex + 1,
                                },
                                RightLines = new List<int>
                                {
                                    srcB.LineIndex + 1,
                                    tgtB.LineIndex + 1,
                                },
                                Severity = "high",
                            }
                        );

                        break; 
                    }
                }
            }

            similarityScore = Math.Min(100.0, ((double)edgeMatches / dataEdgesA.Count) * 100.0);
            similarityScore = Math.Round(similarityScore, 2);

            return matches;
        }
    }
}
