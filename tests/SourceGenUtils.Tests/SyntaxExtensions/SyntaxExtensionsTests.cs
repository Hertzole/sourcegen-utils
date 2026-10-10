using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SyntaxExtensionsWrapper = Hertzole.SourceGen.Wrappers.SyntaxExtensions;
using static SourceGenUtils.Tests.RoslynHelper;

namespace SourceGenUtils.Tests;

public class SyntaxExtensionsTests : GeneratorTests
{
    private const string ATTRIBUTE_SOURCE =
        """
        using System;

        namespace Test.Tester
        {
            [Serializable]
            public class TestClass { }
        }
        """;

    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "SyntaxExtensions";
    }

    [Test]
    public void GetAttributeSymbol_ValidAttribute_ReturnsAttributeSymbol()
    {
        // Arrange
        SyntaxExtensionsWrapper extensions = CompileWrapper("SyntaxExtensions.GetAttributeSymbol(null, null);");
        (AttributeSyntax syntax, SemanticModel semanticModel) = CompileAttribute(ATTRIBUTE_SOURCE);

        // Act
        INamedTypeSymbol? result = extensions.GetAttributeSymbol(syntax, semanticModel);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("SerializableAttribute"));
        Assert.That(result!.ContainingNamespace.ToString(), Is.EqualTo("System"));
    }

    [Test]
    public void GetAttributeSymbol_UnknownAttribute_ReturnsNull()
    {
        // Arrange
        SyntaxExtensionsWrapper extensions = CompileWrapper("SyntaxExtensions.GetAttributeSymbol(null, null);");
        (AttributeSyntax syntax, SemanticModel semanticModel) = CompileAttribute(
            """
            namespace Test.Tester
            {
                [DoesNotExist]
                public class TestClass { }
            }
            """);

        // Act
        INamedTypeSymbol? result = extensions.GetAttributeSymbol(syntax, semanticModel);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetAttributeSymbol_CancelledToken_Throws()
    {
        // Arrange
        SyntaxExtensionsWrapper extensions = CompileWrapper("SyntaxExtensions.GetAttributeSymbol(null, null);");
        (AttributeSyntax syntax, SemanticModel semanticModel) = CompileAttribute(ATTRIBUTE_SOURCE);

        // Act
        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
            extensions.GetAttributeSymbol(syntax, semanticModel, new CancellationToken(true)))!;

        // Assert
        Assert.That(ex.InnerException, Is.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void TryGetFieldDeclaration_FieldDeclarationNode_ReturnsTrue()
    {
        // Arrange
        SyntaxExtensionsWrapper extensions = CompileWrapper("SyntaxExtensions.TryGetFieldDeclaration(null, out var field);");
        FieldDeclarationSyntax node = CompileField();

        // Act
        bool result = extensions.TryGetFieldDeclaration(node, out FieldDeclarationSyntax? fieldDeclaration);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(fieldDeclaration, Is.SameAs(node));
    }

    [Test]
    public void TryGetFieldDeclaration_DescendantNode_ReturnsTrue()
    {
        // Arrange
        SyntaxExtensionsWrapper extensions = CompileWrapper("SyntaxExtensions.TryGetFieldDeclaration(null, out var field);");
        CSharpCompilation compilation = GetCompilation(CompileFieldSource());
        VariableDeclaratorSyntax node = compilation.SyntaxTrees.Single().GetRoot()
                                                   .DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();

        // Act
        bool result = extensions.TryGetFieldDeclaration(node, out FieldDeclarationSyntax? fieldDeclaration);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(fieldDeclaration, Is.Not.Null);
        Assert.That(fieldDeclaration!.Declaration.Variables, Does.Contain(node));
    }

    [Test]
    public void TryGetFieldDeclaration_NoFieldAncestor_ReturnsFalse()
    {
        // Arrange
        SyntaxExtensionsWrapper extensions = CompileWrapper("SyntaxExtensions.TryGetFieldDeclaration(null, out var field);");
        CSharpCompilation compilation = GetCompilation(CompileFieldSource());
        ClassDeclarationSyntax node = compilation.SyntaxTrees.Single().GetRoot()
                                                 .DescendantNodes().OfType<ClassDeclarationSyntax>().Single();

        // Act
        bool result = extensions.TryGetFieldDeclaration(node, out FieldDeclarationSyntax? fieldDeclaration);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(fieldDeclaration, Is.Null);
    }

    [Test]
    public void TryGetFieldDeclaration_CancelledToken_Throws()
    {
        // Arrange
        SyntaxExtensionsWrapper extensions = CompileWrapper("SyntaxExtensions.TryGetFieldDeclaration(null, out var field);");
        CSharpCompilation compilation = GetCompilation(CompileFieldSource());
        ClassDeclarationSyntax node = compilation.SyntaxTrees.Single().GetRoot()
                                                 .DescendantNodes().OfType<ClassDeclarationSyntax>().Single();

        // Act
        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() =>
            extensions.TryGetFieldDeclaration(node, out FieldDeclarationSyntax? fieldDeclaration,
                new CancellationToken(true)))!;

        // Assert
        Assert.That(ex.InnerException, Is.InstanceOf<OperationCanceledException>());
    }

    private static SyntaxExtensionsWrapper CompileWrapper(string usage)
    {
        return new SyntaxExtensionsWrapper(CompileGeneratedTypeByUsing("SyntaxExtensions", usage));
    }

    private static (AttributeSyntax Syntax, SemanticModel SemanticModel) CompileAttribute(string source)
    {
        CSharpCompilation compilation = GetCompilation(source);
        AttributeSyntax syntax = compilation.SyntaxTrees.Single().GetRoot()
                                            .DescendantNodes().OfType<AttributeSyntax>().Single();

        return (syntax, compilation.GetSemanticModel(syntax.SyntaxTree));
    }

    private static FieldDeclarationSyntax CompileField()
    {
        return GetCompilation(CompileFieldSource()).SyntaxTrees.Single().GetRoot()
                                                   .DescendantNodes().OfType<FieldDeclarationSyntax>().Single();
    }

    private static string CompileFieldSource()
    {
        return """
               namespace Test.Tester
               {
                   public class TestClass
                   {
                       public int value = 5;
                   }
               }
               """;
    }
}