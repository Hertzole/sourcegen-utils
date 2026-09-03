using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

public static class RoslynHelper
{
    private const string MAIN = """
                                public static class Program
                                {
                                    public static void Main(string[] args) { }
                                }
                                """;

    public static INamedTypeSymbol CompileTypeToSymbol(string source)
    {
        TypeDeclarationSyntax syntax = CompileTypeToSyntaxInternal(source, out CSharpCompilation compilation);
        SemanticModel semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);

        return semanticModel.GetDeclaredSymbol(syntax)!;
    }

    public static TypeDeclarationSyntax CompileTypeToSyntax(string source)
    {
        return CompileTypeToSyntaxInternal(source, out _);
    }

    public static void AssertIsValidCompilation(params string[] sources)
    {
        CSharpCompilation compilation = GetCompilation([MAIN, .. sources]);

        AssertNoErrorsInCompilation(compilation);
    }

    private static void AssertNoErrorsInCompilation(CSharpCompilation compilation)
    {
        ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();

        for (int i = 0; i < diagnostics.Length; i++)
        {
            if (diagnostics[i].Severity == DiagnosticSeverity.Error)
            {
                Assert.Fail($"Not valid compilation: {diagnostics[i]}");
            }
        }
    }

    private static TypeDeclarationSyntax CompileTypeToSyntaxInternal(string source, out CSharpCompilation compilation)
    {
        compilation = GetCompilation(MAIN, source);
        AssertNoErrorsInCompilation(compilation);
        SyntaxTree tree = compilation.SyntaxTrees.Single(static x => x.ToString() != MAIN);
        return tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Single();
    }

    private static CSharpCompilation GetCompilation(params string[] sources)
    {
        SyntaxTree[] trees = sources.Select(static x => CSharpSyntaxTree.ParseText(x)).ToArray();

        return CSharpCompilation.Create("Tests", trees,
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        ]);
    }
}