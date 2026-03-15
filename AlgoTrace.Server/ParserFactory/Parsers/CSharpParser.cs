using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AlgoTrace.Server.Interfaces;
using AlgoTrace.Server.Models.Tree;

namespace AlgoTrace.Server.ParserFactory.Parsers
{
    public class CSharpParser : ICodeParser
    {
        public string Language => "csharp";

        public UniversalNode Parse(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();
            return MapToUniversal(root);
        }

        private UniversalNode MapToUniversal(SyntaxNode node)
        {
            return new UniversalNode
            {
                Type = MapKind(node.Kind()),
                Value = GetNodeValue(node),
                Children = node.ChildNodes().Select(MapToUniversal).ToList()
            };
        }

        private string MapKind(SyntaxKind kind)
        {
            return kind switch
            {
                SyntaxKind.CompilationUnit => UniversalNodeType.Program,
                SyntaxKind.ClassDeclaration => UniversalNodeType.Class,
                SyntaxKind.StructDeclaration => UniversalNodeType.Class,
                SyntaxKind.MethodDeclaration => UniversalNodeType.Method,
                SyntaxKind.Parameter => UniversalNodeType.Parameter,
                SyntaxKind.VariableDeclarator => UniversalNodeType.VariableDecl,
                SyntaxKind.LocalDeclarationStatement => UniversalNodeType.VariableDecl,

                SyntaxKind.IfStatement => UniversalNodeType.If,
                SyntaxKind.ForStatement => UniversalNodeType.Loop,
                SyntaxKind.ForEachStatement => UniversalNodeType.Loop,
                SyntaxKind.WhileStatement => UniversalNodeType.Loop,
                SyntaxKind.DoStatement => UniversalNodeType.Loop,
                SyntaxKind.SwitchStatement => UniversalNodeType.Switch,
                SyntaxKind.ReturnStatement => UniversalNodeType.Return,
                SyntaxKind.TryStatement => UniversalNodeType.TryCatch,

                SyntaxKind.SimpleAssignmentExpression => UniversalNodeType.Assignment,
                SyntaxKind.InvocationExpression => UniversalNodeType.MethodCall,

                SyntaxKind.AddExpression or SyntaxKind.SubtractExpression or
                SyntaxKind.MultiplyExpression or SyntaxKind.DivideExpression or
                SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression or
                SyntaxKind.LogicalAndExpression or SyntaxKind.LogicalOrExpression
                    => UniversalNodeType.BinaryOperation,

                SyntaxKind.IdentifierName => UniversalNodeType.Identifier,
                SyntaxKind.StringLiteralExpression or SyntaxKind.NumericLiteralExpression
                    => UniversalNodeType.Literal,

                _ => kind.ToString()
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
            if (node is IdentifierNameSyntax id)
                return id.Identifier.Text;
            if (node is LiteralExpressionSyntax literal)
                return literal.Token.ValueText;

            return string.Empty;
        }
    }
}