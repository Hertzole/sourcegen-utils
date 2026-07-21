using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

internal class SyntaxExtensionsTests : GeneratorTests
{
    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "SyntaxExtensions";
    }

    /// <inheritdoc />
    protected override string GetTypeOutline()
    {
        return """
               internal static partial class SyntaxExtensions
               {
               }
               """;
    }

    /// <inheritdoc />
    protected override string[] GetShellMethods()
    {
        return
        [
            "GetAttributeSymbol(Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax, Microsoft.CodeAnalysis.SemanticModel, System.Threading.CancellationToken)",
            "TryGetFieldDeclaration(Microsoft.CodeAnalysis.SyntaxNode, Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax?, System.Threading.CancellationToken)"
        ];
    }

    [Test]
    public void GetAttributeSymbol_Content()
    {
        string content =
            GetMethodContent(
                "SyntaxExtensions.GetAttributeSymbol(Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax, Microsoft.CodeAnalysis.SemanticModel, System.Threading.CancellationToken)");

        const string expected = """
                                public static partial global::Microsoft.CodeAnalysis.INamedTypeSymbol? GetAttributeSymbol(this global::Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax syntax, global::Microsoft.CodeAnalysis.SemanticModel semanticModel, global::System.Threading.CancellationToken cancellationToken)
                                {
                                    if (global::Microsoft.CodeAnalysis.CSharpExtensions.GetSymbolInfo(semanticModel, syntax).Symbol is not global::Microsoft.CodeAnalysis.IMethodSymbol methodSymbol)
                                    {
                                        return null;
                                    }

                                    cancellationToken.ThrowIfCancellationRequested();

                                    if (methodSymbol.ContainingType is global::Microsoft.CodeAnalysis.INamedTypeSymbol attributeSymbol)
                                    {
                                        return attributeSymbol;
                                    }

                                    return null;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void TryGetFieldDeclaration_Content()
    {
        string content =
            GetMethodContent(
                "SyntaxExtensions.TryGetFieldDeclaration(Microsoft.CodeAnalysis.SyntaxNode, Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax?, System.Threading.CancellationToken)");

        string expected = """
                          public static partial bool TryGetFieldDeclaration(this global::Microsoft.CodeAnalysis.SyntaxNode node, out global::Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax? fieldDeclaration, global::System.Threading.CancellationToken cancellationToken)
                          {
                              if (global::Microsoft.CodeAnalysis.CSharpExtensions.IsKind(node, global::Microsoft.CodeAnalysis.CSharp.SyntaxKind.FieldDeclaration))
                              {
                                  fieldDeclaration = (global::Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax) node;
                                  return true;
                              }

                              global::Microsoft.CodeAnalysis.SyntaxNode? parent = node.Parent;
                              while (parent != null)
                              {
                                  cancellationToken.ThrowIfCancellationRequested();
                                  if (global::Microsoft.CodeAnalysis.CSharpExtensions.IsKind(parent, global::Microsoft.CodeAnalysis.CSharp.SyntaxKind.FieldDeclaration))
                                  {
                                      fieldDeclaration = (global::Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax) parent;
                                      return true;
                                  }

                                  parent = parent.Parent;
                              }

                              fieldDeclaration = null;
                              return false;
                          }
                          """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void GetAttributeSymbol_Content_NotCalled()
    {
        const string expected = """
                                public static partial global::Microsoft.CodeAnalysis.INamedTypeSymbol? GetAttributeSymbol(this global::Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax syntax, global::Microsoft.CodeAnalysis.SemanticModel semanticModel, global::System.Threading.CancellationToken cancellationToken)
                                {
                                    return null;
                                }
                                """;

        EmptyContentTest(
            "SyntaxExtensions.GetAttributeSymbol(Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax, Microsoft.CodeAnalysis.SemanticModel, System.Threading.CancellationToken)",
            expected);
    }

    [Test]
    public void TryGetFieldDeclaration_Content_NotCalled()
    {
        const string expected = """
                                public static partial bool TryGetFieldDeclaration(this global::Microsoft.CodeAnalysis.SyntaxNode node, out global::Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax? fieldDeclaration, global::System.Threading.CancellationToken cancellationToken)
                                {
                                    fieldDeclaration = null; return false;
                                }
                                """;

        EmptyContentTest(
            "SyntaxExtensions.TryGetFieldDeclaration(Microsoft.CodeAnalysis.SyntaxNode, Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax?, System.Threading.CancellationToken)",
            expected);
    }

    [Test]
    public void Call_GetAttributeSymbol_AsStatic()
    {
        string[] expectedMethods =
        [
            "SyntaxExtensions.GetAttributeSymbol(Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax, Microsoft.CodeAnalysis.SemanticModel, System.Threading.CancellationToken)"
        ];

        MetadataReference[] refs =
        [
            MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(AttributeSyntax).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(SemanticModel).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location)
        ];

        AssertCallingMethodCreatesMethods(writer =>
        {
            writer.AppendLine("using Hertzole.SourceGen;");
            writer.AppendLine("using Microsoft.CodeAnalysis.CSharp.Syntax;");
            writer.AppendLine();
            writer.AppendLine("class Test");
            using (writer.WithBlock())
            {
                writer.AppendLine("void Method()");
                using (writer.WithBlock())
                {
                    writer.AppendLine("SyntaxExtensions.GetAttributeSymbol(null!, null!, default);");
                }
            }
        }, refs, expectedMethods);
    }

    [Test]
    public void Call_TryGetFieldDeclaration_AsStatic()
    {
        string[] expectedMethods =
        [
            "SyntaxExtensions.TryGetFieldDeclaration(Microsoft.CodeAnalysis.SyntaxNode, Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax?, System.Threading.CancellationToken)"
        ];

        MetadataReference[] refs =
        [
            MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(SyntaxNode).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location)
        ];

        AssertCallingMethodCreatesMethods(writer =>
        {
            writer.AppendLine("using Hertzole.SourceGen;");
            writer.AppendLine("using Microsoft.CodeAnalysis;");
            writer.AppendLine();
            writer.AppendLine("class Test");
            using (writer.WithBlock())
            {
                writer.AppendLine("void Method()");
                using (writer.WithBlock())
                {
                    writer.AppendLine("SyntaxExtensions.TryGetFieldDeclaration(null!, out var result);");
                }
            }
        }, refs, expectedMethods);
    }
}