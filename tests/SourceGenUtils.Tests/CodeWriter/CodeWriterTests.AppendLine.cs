using System;
using Hertzole.SourceGen.Wrappers;
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
        CodeWriter writer = CompileCodeWriter(false,
            "new CodeWriter().AppendLine((Microsoft.CodeAnalysis.ITypeSymbol)null!, true, true).ToString();");

        // Act
        writer.AppendLine(symbol, isPartial, includeNamespace);
        return writer.ToString();
    }
}