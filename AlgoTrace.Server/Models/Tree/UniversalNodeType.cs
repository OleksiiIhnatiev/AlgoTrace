namespace AlgoTrace.Server.Models.Tree
{
    public static class UniversalNodeType
    {
        public const string Program = "Program";
        public const string Class = "Class";
        public const string Method = "Method";
        public const string Parameter = "Parameter";
        public const string VariableDecl = "VariableDecl";

        public const string If = "If";
        public const string Loop = "Loop";
        public const string Switch = "Switch";
        public const string Return = "Return";
        public const string TryCatch = "TryCatch";

        public const string Assignment = "Assignment";
        public const string MethodCall = "MethodCall";
        public const string BinaryOperation = "BinaryOperation";
        public const string UnaryOperation = "UnaryOperation";

        public const string Identifier = "Identifier";
        public const string Literal = "Literal";
        public const string Unknown = "Unknown";
    }
}