using System;
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
    [TestCase("interface", true, ExpectedResult = "partial interface")]
    [TestCase("interface", false, ExpectedResult = "interface")]
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
    public void HasAttribute_Generic()
    {
        // Arrange
        SymbolExtensions extensions = CompileWrapper("SymbolExtensions.HasAttribute<SerializableAttribute>(null);");
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
        bool result = extensions.HasAttribute<SerializableAttribute>(symbol);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void HasAttribute_Type()
    {
        // Arrange
        SymbolExtensions extensions = CompileWrapper("SymbolExtensions.HasAttribute(null, typeof(object));");
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
        bool result = extensions.HasAttribute(symbol, typeof(SerializableAttribute));

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void HasAttribute_GenericAndType()
    {
        // Arrange
        SymbolExtensions extensions =
            CompileWrapper("SymbolExtensions.HasAttribute(null, typeof(object)); SymbolExtensions.HasAttribute<SerializableAttribute>(null);");

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
        bool genericResult = extensions.HasAttribute<SerializableAttribute>(symbol);
        bool typeResult = extensions.HasAttribute(symbol, typeof(SerializableAttribute));

        // Assert
        Assert.That(genericResult, Is.True);
        Assert.That(typeResult, Is.True);
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

    [Test]
    public void TryGetAttribute_Generic()
    {
        // Arrange
        SymbolExtensions extensions = CompileWrapper("SymbolExtensions.TryGetAttribute<SerializableAttribute>(null, out var attribute);");
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
        bool result = extensions.TryGetAttribute<SerializableAttribute>(symbol, out AttributeData? attribute);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(attribute, Is.Not.Null);
        Assert.That(attribute!.AttributeClass, Is.Not.Null);
        Assert.That(attribute.AttributeClass!.Name, Is.EqualTo("SerializableAttribute"));
        Assert.That(attribute.AttributeClass!.ContainingNamespace!.Name, Is.EqualTo("System"));
    }

    [Test]
    public void TryGetAttribute_Type()
    {
        // Arrange
        SymbolExtensions extensions = CompileWrapper("SymbolExtensions.TryGetAttribute(null, typeof(SerializableAttribute), out var attribute);");
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
        bool result = extensions.TryGetAttribute(symbol, typeof(SerializableAttribute), out AttributeData? attribute);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(attribute, Is.Not.Null);
        Assert.That(attribute!.AttributeClass, Is.Not.Null);
        Assert.That(attribute.AttributeClass!.Name, Is.EqualTo("SerializableAttribute"));
        Assert.That(attribute.AttributeClass!.ContainingNamespace!.Name, Is.EqualTo("System"));
    }

    [Test]
    public void TryGetAttribute_GenericAndType()
    {
        // Arrange
        SymbolExtensions extensions =
            CompileWrapper(
                "SymbolExtensions.TryGetAttribute<SerializableAttribute>(null, out _); SymbolExtensions.TryGetAttribute(null, typeof(SerializableAttribute), out var attribute);");

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
        bool genericResult = extensions.TryGetAttribute<SerializableAttribute>(symbol, out AttributeData? genericAttribute);
        bool typeResult = extensions.TryGetAttribute(symbol, typeof(SerializableAttribute), out AttributeData? typeAttribute);

        // Assert
        DoAssert(genericResult, genericAttribute);
        DoAssert(typeResult, typeAttribute);

        static void DoAssert(bool result, AttributeData? attribute)
        {
            Assert.That(result, Is.True);
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute!.AttributeClass, Is.Not.Null);
            Assert.That(attribute.AttributeClass!.Name, Is.EqualTo("SerializableAttribute"));
            Assert.That(attribute.AttributeClass!.ContainingNamespace!.Name, Is.EqualTo("System"));
        }
    }

    private static SymbolExtensions CompileWrapper(string usage)
    {
        return new SymbolExtensions(CompileGeneratedTypeByUsing("SymbolExtensions", usage));
    }
}