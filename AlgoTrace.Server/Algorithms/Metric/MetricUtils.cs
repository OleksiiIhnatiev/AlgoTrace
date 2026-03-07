using System.Text.RegularExpressions;

namespace AlgoTrace.Server.Algorithms.Metric
{
    public class HalsteadResult
    {
        public int DistinctOperators { get; set; }
        public int DistinctOperands { get; set; }
        public int TotalOperators { get; set; }
        public int TotalOperands { get; set; }

        public double Vocabulary => DistinctOperators + DistinctOperands;
        public double Length => TotalOperators + TotalOperands;
        public double Volume => Length * (Vocabulary > 0 ? Math.Log2(Vocabulary) : 0);
        public double Difficulty => (DistinctOperands > 0) ? (DistinctOperators / 2.0) * (TotalOperands / (double)DistinctOperands) : 0;
        public double Effort => Difficulty * Volume;
    }

    public static class MetricUtils
    {
        private static readonly HashSet<string> Operators = new()
        {
            "+", "-", "*", "/", "%", "**", "//", "=", "+=", "-=", "*=", "/=",
            "==", "!=", "<", ">", "<=", ">=", "and", "or", "not", "is", "in",
            "if", "else", "elif", "for", "while", "def", "return", "class", 
            "import", "from", "try", "except", "finally", "raise", "assert",
            "(", ")", "[", "]", "{", "}", ":", ",", "."
        };

        public static int CalculateMcCabeComplexity(string code)
        {
            int complexity = 1;
            
            var branchingPatterns = new[]
            {
                @"\bif\b", @"\belif\b", @"\bfor\b", @"\bwhile\b", 
                @"\bexcept\b", @"\bwith\b", @"\bassert\b"
            };

            var boolOperators = new[] { @"\band\b", @"\bor\b" };

            foreach (var pattern in branchingPatterns.Concat(boolOperators))
            {
                complexity += Regex.Matches(code, pattern).Count;
            }

            return complexity;
        }

        public static HalsteadResult CalculateHalsteadMetrics(string code)
        {
            var result = new HalsteadResult();
            var operatorCounts = new Dictionary<string, int>();
            var operandCounts = new Dictionary<string, int>();

            var tokens = Regex.Split(code, @"(\s+|[(){}\[\],:;+\-*/%&|^!=<>])")
                              .Where(t => !string.IsNullOrWhiteSpace(t))
                              .ToList();

            foreach (var token in tokens)
            {
                if (Operators.Contains(token))
                {
                    if (!operatorCounts.ContainsKey(token)) operatorCounts[token] = 0;
                    operatorCounts[token]++;
                }
                else
                {
                    if (token.Length > 0 && !token.StartsWith("#"))
                    {
                        if (token.StartsWith("\"") || token.StartsWith("'"))
                        {
                            if (!operandCounts.ContainsKey("LITERAL_STRING")) operandCounts["LITERAL_STRING"] = 0;
                            operandCounts["LITERAL_STRING"]++;
                        }
                        else
                        {
                            if (!operandCounts.ContainsKey(token)) operandCounts[token] = 0;
                            operandCounts[token]++;
                        }
                    }
                }
            }

            result.DistinctOperators = operatorCounts.Count;
            result.DistinctOperands = operandCounts.Count;
            result.TotalOperators = operatorCounts.Values.Sum();
            result.TotalOperands = operandCounts.Values.Sum();

            return result;
        }
    }
}