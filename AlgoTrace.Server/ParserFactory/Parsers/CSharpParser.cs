using AlgoTrace.Server.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AlgoTrace.Server.ParserFactory.Parsers
{
    public class CSharpParser : ICodeParser
    {
        public string Language => "csharp";

        public UniversalNode Parse(string code)
        {
            var root = CSharpSyntaxTree.ParseText(code).GetRoot();
            return MapToUniversal(root);
        }

        private UniversalNode MapToUniversal(SyntaxNode node)
        {
            return new UniversalNode
            {
                Type = node.Kind().ToString(),
                Value = GetNodeValue(node),
                Children = node.ChildNodes().Select(MapToUniversal).ToList()
            };
        }

        private string GetNodeValue(SyntaxNode node)
        {
            if (node is MethodDeclarationSyntax method)
                return method.Identifier.Text;

            if (node is VariableDeclaratorSyntax variable)
                return variable.Identifier.Text;

            if (node is ClassDeclarationSyntax classDecl)
                return classDecl.Identifier.Text;

            return "";
        }
    }
}