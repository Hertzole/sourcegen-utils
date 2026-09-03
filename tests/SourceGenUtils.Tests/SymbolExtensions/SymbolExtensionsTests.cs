using System;
using System.Reflection;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using static SourceGenUtils.Tests.RoslynHelper;

namespace SourceGenUtils.Tests;

public class SymbolExtensionsTests : GeneratorTests
{
    private const string MS_CODE = "Microsoft.CodeAnalysis";

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
        SymbolExtensionsWrapper wrapper = CompileWrapper($"GetDeclarationString({MS_CODE}.ITypeSymbol, bool)");
        string source = $"public {declaration} MyType {{}}";
        INamedTypeSymbol symbol = CompileTypeToSymbol(source);

        // Act
        string result = wrapper.GetDeclarationString(symbol, isPartial);
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
        SymbolExtensionsWrapper wrapper = CompileWrapper($"HasAttribute({MS_CODE}.ISymbol, string)");
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
        return wrapper.HasAttribute(symbol, attributeName);
    }

    [Test]
    public void HasAttribute_NoAttributes()
    {
        // Arrange
        SymbolExtensionsWrapper wrapper = CompileWrapper("HasAttribute(Microsoft.CodeAnalysis.ISymbol, string)");
        const string source = """
                              using System;

                              namespace Test.Tester
                              {
                                  public class TestClass { }
                              }
                              """;

        INamedTypeSymbol symbol = CompileTypeToSymbol(source);

        // Act
        bool result = wrapper.HasAttribute(symbol, "System.SerializableAttribute");

        // Assert
        Assert.That(result, Is.False);
    }

    private static SymbolExtensionsWrapper CompileWrapper(params string[] calledMethods)
    {
        return new SymbolExtensionsWrapper(CompileGeneratedType("SymbolExtensions", calledMethods));
    }

    private class SymbolExtensionsWrapper
    {
        private readonly MethodInfo getDeclarationString;
        private readonly MethodInfo hasAttribute;

        public SymbolExtensionsWrapper(Type type)
        {
            getDeclarationString = GetMethod(type, "GetDeclarationString", BindingFlags.Public | BindingFlags.Static, typeof(ITypeSymbol), typeof(bool));
            hasAttribute = GetMethod(type, "HasAttribute", BindingFlags.Public | BindingFlags.Static, typeof(ISymbol), typeof(string));
        }

        public string GetDeclarationString(ITypeSymbol symbol, bool isPartial)
        {
            return getDeclarationString.InvokeStatic<string>(symbol, isPartial);
        }

        public bool HasAttribute(ISymbol symbol, string attributeName)
        {
            return hasAttribute.InvokeStatic<bool>(symbol, attributeName);
        }
    }
}