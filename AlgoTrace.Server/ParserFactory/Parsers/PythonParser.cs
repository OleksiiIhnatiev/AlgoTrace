using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.ParserFactory;
using System.Text.RegularExpressions;

public class PythonParser : ICodeParser
{
    public string Language => "python";

    public UniversalNode Parse(string code)
    {
        var root = new UniversalNode { Type = "Module" };
        var lines = code.Split('\n');

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.Trim().StartsWith("def "))
                root.Children.Add(new UniversalNode { Type = "FunctionDefinition", Value = line.Trim() });
            else if (line.Trim().StartsWith("if "))
                root.Children.Add(new UniversalNode { Type = "IfStatement" });
            else if (line.Trim().StartsWith("while ") || line.Trim().StartsWith("for "))
                root.Children.Add(new UniversalNode { Type = "Loop" });
        }
        return root;
    }
}