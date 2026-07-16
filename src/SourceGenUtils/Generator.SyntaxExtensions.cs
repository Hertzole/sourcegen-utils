using System.Collections.Generic;

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
                Summary = "Extension methods for working with <see cref=\"global::Microsoft.CodeAnalysis\"/> syntax nodes."
            },
            Methods =
            [
                new MethodSource
                {
                    Name = "GetAttributeSymbol",
                    Signature =
                        "public static partial global::Microsoft.CodeAnalysis.INamedTypeSymbol? GetAttributeSymbol(this global::Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax syntax, global::Microsoft.CodeAnalysis.SemanticModel semanticModel, global::System.Threading.CancellationToken cancellationToken = default)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(
                            "if (global::Microsoft.CodeAnalysis.CSharpExtensions.GetSymbolInfo(semanticModel, syntax).Symbol is not global::Microsoft.CodeAnalysis.IMethodSymbol methodSymbol)");

                        using (writer.WithBlock())
                        {
                            writer.AppendLine("return null;");
                        }

                        writer.AppendLine();
                        writer.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
                        writer.AppendLine();

                        writer.AppendLine("if (methodSymbol.ContainingType is global::Microsoft.CodeAnalysis.INamedTypeSymbol attributeSymbol)");
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
                        Summary = "Gets the <see cref=\"global::Microsoft.CodeAnalysis.INamedTypeSymbol\"/> for the specified attribute syntax.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["syntax"] = "The attribute syntax node.",
                            ["semanticModel"] = "The semantic model for the syntax tree.",
                            ["cancellationToken"] = "A cancellation token."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "TryGetFieldDeclaration",
                    Signature =
                        "public static partial bool TryGetFieldDeclaration(this global::Microsoft.CodeAnalysis.SyntaxNode node, out global::Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax? fieldDeclaration, " +
                        "global::System.Threading.CancellationToken cancellationToken = default)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(
                            "if (global::Microsoft.CodeAnalysis.CSharpExtensions.IsKind(node, global::Microsoft.CodeAnalysis.CSharp.SyntaxKind.FieldDeclaration))");

                        using (writer.WithBlock())
                        {
                            writer.AppendLine("fieldDeclaration = (global::Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax) node;");
                            writer.AppendLine("return true;");
                        }

                        writer.AppendLine();
                        writer.AppendLine("global::Microsoft.CodeAnalysis.SyntaxNode? parent = node.Parent;");
                        writer.AppendLine("while (parent != null)");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("cancellationToken.ThrowIfCancellationRequested();");
                            writer.AppendLine(
                                "if (global::Microsoft.CodeAnalysis.CSharpExtensions.IsKind(parent, global::Microsoft.CodeAnalysis.CSharp.SyntaxKind.FieldDeclaration))");

                            using (writer.WithBlock())
                            {
                                writer.AppendLine("fieldDeclaration = (global::Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax) parent;");
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
                            "Attempts to find a <see cref=\"global::Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax\"/> from the specified node or its ancestors.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["node"] = "The syntax node to search from.",
                            ["fieldDeclaration"] = "When this method returns, contains the field declaration if found; otherwise, <c>null</c>.",
                            ["cancellationToken"] = "A cancellation token."
                        }
                    }
                }
            ]
        };
    }
}