using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private static TypeSource CreateSyntaxExtensions()
    {
        return new TypeSource
        {
            Signature = "internal static partial class SyntaxExtensions",
            Trivia = new TriviaSource
            {
                Summary = "Extension methods for working with syntax nodes."
            },
            Methods =
            [
                new MethodSource
                {
                    Name = "GetAttributeSymbol",
                    Signature =
                        $"public static partial {GLOBAL_MS_CODE}.INamedTypeSymbol? GetAttributeSymbol(this {GLOBAL_MS_CODE}.CSharp.Syntax.AttributeSyntax syntax, {GLOBAL_MS_CODE}.SemanticModel semanticModel, global::System.Threading.CancellationToken cancellationToken = default)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(
                            $"if ({GLOBAL_MS_CODE}CSharpExtensions.GetSymbolInfo(semanticModel, syntax).Symbol is not {GLOBAL_MS_CODE}.IMethodSymbol methodSymbol)");

                        using (writer.WithBlock())
                        {
                            writer.AppendLine("return null;");
                        }

                        writer.AppendLine();
                        writer.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
                        writer.AppendLine();

                        writer.AppendLine($"if (methodSymbol.ContainingType is {GLOBAL_MS_CODE}.INamedTypeSymbol attributeSymbol)");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("return attributeSymbol;");
                        }

                        writer.AppendLine();
                        writer.AppendLine("return null;");
                    },
                    EmptyStub = "return null;",
                    Trivia = new TriviaSource
                    {
                        Summary = $"Gets the {GetTypeTriviaReference<INamedTypeSymbol>()} for the specified attribute syntax.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["syntax"] = "The attribute syntax node.",
                            ["semanticModel"] = "The semantic model for the syntax tree.",
                            ["cancellationToken"] = "A cancellation token."
                        },
                        Returns = $"The {GetTypeTriviaReference<INamedTypeSymbol>()} for the attribute, or {TRIVIA_NULL} if not found."
                    }
                },
                new MethodSource
                {
                    Name = "TryGetFieldDeclaration",
                    Signature =
                        $"public static partial bool TryGetFieldDeclaration(this {GLOBAL_MS_CODE}.SyntaxNode node, out {GLOBAL_MS_CODE}.CSharp.Syntax.FieldDeclarationSyntax? fieldDeclaration, " +
                        "global::System.Threading.CancellationToken cancellationToken = default)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine($"if ({GLOBAL_MS_CODE}.CSharpExtensions.IsKind(node, {GLOBAL_MS_CODE}.CSharp.SyntaxKind.FieldDeclaration))");

                        using (writer.WithBlock())
                        {
                            writer.AppendLine($"fieldDeclaration = ({GLOBAL_MS_CODE}.CSharp.Syntax.FieldDeclarationSyntax) node;");
                            writer.AppendLine("return true;");
                        }

                        writer.AppendLine();
                        writer.AppendLine($"{GLOBAL_MS_CODE}.SyntaxNode? parent = node.Parent;");
                        writer.AppendLine("while (parent != null)");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
                            writer.AppendLine($"if ({GLOBAL_MS_CODE}.CSharpExtensions.IsKind(parent, {GLOBAL_MS_CODE}.CSharp.SyntaxKind.FieldDeclaration))");

                            using (writer.WithBlock())
                            {
                                writer.AppendLine($"fieldDeclaration = ({GLOBAL_MS_CODE}.CSharp.Syntax.FieldDeclarationSyntax) parent;");
                                writer.AppendLine("return true;");
                            }

                            writer.AppendLine();
                            writer.AppendLine("parent = parent.Parent;");
                        }

                        writer.AppendLine();
                        writer.AppendLine("fieldDeclaration = null;");
                        writer.AppendLine("return false;");
                    },
                    EmptyStub = "fieldDeclaration = null; return false;",
                    Trivia = new TriviaSource
                    {
                        Summary =
                            $"Attempts to find a {GetTypeTriviaReference<FieldDeclarationSyntax>()} from the specified node or its ancestors.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["node"] = "The syntax node to search from.",
                            ["fieldDeclaration"] = $"When this method returns, contains the field declaration if found; otherwise, {TRIVIA_NULL}.",
                            ["cancellationToken"] = "A cancellation token."
                        },
                        Returns = $"{TRIVIA_TRUE} if a field declaration was found; otherwise {TRIVIA_FALSE}."
                    }
                }
            ]
        };
    }
}