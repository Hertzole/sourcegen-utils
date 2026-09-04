using System;
using System.Reflection;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

internal partial class CodeWriterTests
{
    [Test]
    [TestCaseSource(nameof(AppendCases))]
    public void AppendLine<T>(T value, bool isUnsafe)
    {
        CreateAppendTest(AppendType.AppendLine, isUnsafe, value);
    }

    [Test]
    [TestCaseSource(nameof(AppendFormatCases))]
    public void AppendLineFormat<T>(T value, bool isUnsafe) where T : IFormattable
    {
        CreateAppendFormatTest(AppendType.AppendLine, isUnsafe, value);
    }

    [Test]
    [TestCaseSource(nameof(AppendSymbolCases))]
    public string AppendLineSymbol(string source, bool isPartial, bool includeNamespace)
    {
        // Arrange
        INamedTypeSymbol symbol = RoslynHelper.CompileTypeToSymbol(source);
        object writer = CompileCodeWriter(false, "AppendLine(Microsoft.CodeAnalysis.ITypeSymbol, bool, bool)", "ToString()");
        MethodInfo appendMethod = GetMethod(writer.GetType(), "AppendLine", BindingFlags.Public | BindingFlags.Instance, typeof(ITypeSymbol), typeof(bool),
            typeof(bool));

        MethodInfo toStringMethod = GetMethod(writer.GetType(), "ToString", BindingFlags.Public | BindingFlags.Instance);

        // Act
        appendMethod.InvokeInstance(writer, symbol, isPartial, includeNamespace);
        return toStringMethod.InvokeInstance<string>(writer);
    }
}