using Hertzole.SourceGen.Wrappers;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using static SourceGenUtils.Tests.RoslynHelper;

namespace SourceGenUtils.Tests;

public class SymbolExtensionsTests : GeneratorTests
{
    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "SymbolExtensions";
    }

    [Test]
    [TestCase("record", true, ExpectedResult = "partial record")]
    [TestCase("record", false, ExpectedResult = "record")]
    [TestCase("static class", true, ExpectedResult = "static partial class")]
    [TestCase("static class", false, ExpectedResult = "static class")]
    [TestCase("readonly struct", true, ExpectedResult = "readonly partial struct")]
    [TestCase("readonly struct", false, ExpectedResult = "readonly struct")]
    [TestCase("readonly record struct", true, ExpectedResult = "readonly partial record struct")]
    [TestCase("readonly record struct", false, ExpectedResult = "readonly record struct")]
    [TestCase("record struct", true, ExpectedResult = "partial record struct")]
    [TestCase("record struct", false, ExpectedResult = "record struct")]
    [TestCase("struct", true, ExpectedResult = "partial struct")]
    [TestCase("struct", false, ExpectedResult = "struct")]
    public string GetDeclarationString(string declaration, bool isPartial)
    {
        // Arrange
        SymbolExtensions extensions = CompileWrapper("SymbolExtensions.GetDeclarationString(null, false);");
        string source = $"public {declaration} MyType {{}}";
        INamedTypeSymbol symbol = CompileTypeToSymbol(source);

        // Act
        string result = extensions.GetDeclarationString(symbol, isPartial);
        // Just to make sure the result is valid C# code.
        string newSource = $"public {result} MyType {{ }}";

        // Assert
        AssertIsValidCompilation(newSource);

        return result;
    }

    [Test]
    [TestCase("SerializableAttribute", ExpectedResult = false)]
    [TestCase("System.SerializableAttribute", ExpectedResult = true)]
    [TestCase("global::System.SerializableAttribute", ExpectedResult = true)]
    [TestCase("NonSerializableAttribute", ExpectedResult = false)]
    [TestCase("System.NonSerializableAttribute", ExpectedResult = false)]
    [TestCase("global::System.NonSerializableAttribute", ExpectedResult = false)]
    public bool HasAttribute(string attributeName)
    {
        // Arrange
        SymbolExtensions extensions = CompileWrapper("SymbolExtensions.HasAttribute(null, \"\");");
        const string source = """
                              using System;

                              namespace Test.Tester
                              {
                                  [Serializable]
                                  public class TestClass { }
                              }
                              """;

        INamedTypeSymbol symbol = CompileTypeToSymbol(source);

        // Act
        return extensions.HasAttribute(symbol, attributeName);
    }

    [Test]
    public void HasAttribute_NoAttributes()
    {
        // Arrange
        SymbolExtensions extensions = CompileWrapper("SymbolExtensions.HasAttribute(null, \"\");");
        const string source = """
                              using System;

                              namespace Test.Tester
                              {
                                  public class TestClass { }
                              }
                              """;

        INamedTypeSymbol symbol = CompileTypeToSymbol(source);

        // Act
        bool result = extensions.HasAttribute(symbol, "System.SerializableAttribute");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetAttribute_IsAttribute()
    {
        // Arrange
        SymbolExtensions extensions = CompileWrapper("SymbolExtensions.TryGetAttribute(null, \"\", out var attribute);");
        const string source = """
                              using System;

                              namespace Test.Tester
                              {
                                  [Serializable]
                                  public class TestClass { }
                              }
                              """;

        INamedTypeSymbol symbol = CompileTypeToSymbol(source);

        // Act
        bool result = extensions.TryGetAttribute(symbol, "global::System.SerializableAttribute", out AttributeData? attribute);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(attribute, Is.Not.Null);
        Assert.That(attribute!.AttributeClass, Is.Not.Null);
        Assert.That(attribute.AttributeClass!.Name, Is.EqualTo("SerializableAttribute"));
        Assert.That(attribute.AttributeClass!.ContainingNamespace!.Name, Is.EqualTo("System"));
    }

    [Test]
    [TestCase("SerializableAttribute", ExpectedResult = false)]
    [TestCase("NonSerializableAttribute", ExpectedResult = false)]
    [TestCase("System.NonSerializableAttribute", ExpectedResult = false)]
    [TestCase("global::System.NonSerializableAttribute", ExpectedResult = false)]
    public bool TryGetAttribute_InvalidAttribute(string attributeName)
    {
        // Arrange
        SymbolExtensions extensions = CompileWrapper("SymbolExtensions.TryGetAttribute(null, \"\", out var attribute);");
        const string source = """
                              using System;

                              namespace Test.Tester
                              {
                                  [Serializable]
                                  public class TestClass { }
                              }
                              """;

        INamedTypeSymbol symbol = CompileTypeToSymbol(source);

        // Act
        bool result = extensions.TryGetAttribute(symbol, attributeName, out AttributeData? attribute);

        // Assert
        Assert.That(attribute, Is.Null);
        return result;
    }

    private static SymbolExtensions CompileWrapper(string usage)
    {
        return new SymbolExtensions(CompileGeneratedTypeByUsing("SymbolExtensions", usage));
    }
}