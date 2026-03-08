using System.Text.RegularExpressions;

namespace AlgoTrace.Server.Algorithms.Graph
{
    public class GraphNode
    {
        public int Id { get; set; }
        public int LineIndex { get; set; }
        public string Content { get; set; }
        public string Type { get; set; }
        public HashSet<string> Variables { get; set; } = new();
    }

    public class GraphEdge
    {
        public int SourceId { get; set; }
        public int TargetId { get; set; }
        public string Type { get; set; }
    }

    public class CodeGraph
    {
        public List<GraphNode> Nodes { get; set; } = new();
        public List<GraphEdge> Edges { get; set; } = new();
    }

    public static class GraphUtils
    {
        public static CodeGraph BuildGraph(string code, bool includeDataDeps = false)
        {
            var graph = new CodeGraph();
            var lines = code.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var node = new GraphNode
                {
                    Id = i,
                    LineIndex = i,
                    Content = line,
                    Type = GetNodeType(line),
                };

                if (includeDataDeps)
                {
                    var matches = Regex.Matches(line, @"\b[a-zA-Z_][a-zA-Z0-9_]*\b");
                    foreach (Match m in matches)
                        node.Variables.Add(m.Value);
                }

                graph.Nodes.Add(node);
            }

            for (int i = 0; i < graph.Nodes.Count - 1; i++)
            {
                graph.Edges.Add(
                    new GraphEdge
                    {
                        SourceId = graph.Nodes[i].Id,
                        TargetId = graph.Nodes[i + 1].Id,
                        Type = "flow",
                    }
                );
            }

            if (includeDataDeps)
            {
                for (int i = 0; i < graph.Nodes.Count; i++)
                {
                    for (int j = i + 1; j < graph.Nodes.Count; j++)
                    {
                        if (graph.Nodes[i].Variables.Overlaps(graph.Nodes[j].Variables))
                        {
                            graph.Edges.Add(
                                new GraphEdge
                                {
                                    SourceId = graph.Nodes[i].Id,
                                    TargetId = graph.Nodes[j].Id,
                                    Type = "data",
                                }
                            );
                        }
                    }
                }
            }

            return graph;
        }

        private static string GetNodeType(string line) =>
            line.StartsWith("if ") || line.StartsWith("while ") || line.StartsWith("for ")
                ? "control"
            : line.StartsWith("def ") ? "def"
            : "statement";
    }
}
