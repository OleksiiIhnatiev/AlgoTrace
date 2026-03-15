using AlgoTrace.Server.Models.Tree;
using System.Text.RegularExpressions;

namespace AlgoTrace.Server.Algorithms.Graph
{
    public class GraphNode
    {
        public int Id { get; set; }
        public int LineIndex { get; set; }
        public string Content { get; set; } = "";
        public string Type { get; set; } = "";
        public HashSet<string> Variables { get; set; } = new();
    }

    public class GraphEdge
    {
        public int SourceId { get; set; }
        public int TargetId { get; set; }
        public string Type { get; set; } = "";
    }

    public class CodeGraph
    {
        public List<GraphNode> Nodes { get; set; } = new();
        public List<GraphEdge> Edges { get; set; } = new();
    }

    public static class GraphUtils
    {
        public static CodeGraph BuildGraph(UniversalNode rootNode, bool includeDataDeps = false)
        {
            var graph = new CodeGraph();
            if (rootNode == null) return graph;

            int idCounter = 0;

            int Traverse(UniversalNode node, int parentId = -1)
            {
                int currentId = idCounter++;

                var gNode = new GraphNode
                {
                    Id = currentId,
                    LineIndex = currentId,
                    Content = string.IsNullOrWhiteSpace(node.Value) ? node.Type : node.Value,
                    Type = node.Type
                };

                if (includeDataDeps && !string.IsNullOrWhiteSpace(gNode.Content))
                {
                    var matches = Regex.Matches(gNode.Content, @"\b[a-zA-Z_][a-zA-Z0-9_]*\b");
                    foreach (Match m in matches)
                        gNode.Variables.Add(m.Value);
                }

                graph.Nodes.Add(gNode);

                if (parentId != -1)
                {
                    graph.Edges.Add(new GraphEdge
                    {
                        SourceId = parentId,
                        TargetId = currentId,
                        Type = "hierarchy"
                    });
                }

                int previousChildId = -1;
                foreach (var child in node.Children)
                {
                    int childId = Traverse(child, currentId);

                    if (previousChildId != -1)
                    {
                        graph.Edges.Add(new GraphEdge
                        {
                            SourceId = previousChildId,
                            TargetId = childId,
                            Type = "flow"
                        });
                    }
                    previousChildId = childId;
                }

                return currentId;
            }

            Traverse(rootNode);

            if (includeDataDeps)
            {
                for (int i = 0; i < graph.Nodes.Count; i++)
                {
                    for (int j = i + 1; j < graph.Nodes.Count; j++)
                    {
                        if (graph.Nodes[i].Variables.Count > 0 &&
                            graph.Nodes[i].Variables.Overlaps(graph.Nodes[j].Variables))
                        {
                            graph.Edges.Add(new GraphEdge
                            {
                                SourceId = graph.Nodes[i].Id,
                                TargetId = graph.Nodes[j].Id,
                                Type = "data"
                            });
                        }
                    }
                }
            }

            return graph;
        }
    }
}