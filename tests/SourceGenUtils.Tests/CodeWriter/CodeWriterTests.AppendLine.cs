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

    [Test]
    public void AppendLineArrayBuilder()
    {
        // Arrange
        string message = Fake.Lorem.Sentence();
        string otherLine = Fake.Lorem.Sentence();
        CodeWriter writer = CompileCodeWriter(false,
            "var builder = new ArrayBuilder<char>(); builder.Add('a'); new CodeWriter().AppendLine(builder).Append(\"\").ToString();");

        string expected = message + "\n" + otherLine;

        ArrayBuilder<char> arrayBuilder = new ArrayBuilder<char>(writer.Type.Assembly);

        // Act
        // Because we can't pass ReadOnlySpan in args, add each letter instead.
        for (int i = 0; i < message.Length; i++)
        {
            arrayBuilder.Add(message[i]);
        }

        CodeWriter returnedValue = writer.AppendLine(arrayBuilder).Append(otherLine);

        // Assert
        AssertWriter(expected, writer, returnedValue);
    }

    [Test]
    public void AppendLineCharRepeat([Values] bool isUnsafe)
    {
        // Arrange
        char value = Fake.Random.Char();
        string otherLine = Fake.Lorem.Sentence();
        int repeatCount = Fake.Random.Int(5, 10);
        string expected = new string(value, repeatCount) + "\n" + otherLine;
        CodeWriter writer = CompileCodeWriter(isUnsafe, "new CodeWriter().AppendLine('a', 1).Append(\"\").ToString();");

        // Act
        CodeWriter returnedValue = writer.AppendLine(value, repeatCount).Append(otherLine);

        // Assert
        AssertWriter(expected, writer, returnedValue);
    }

    [Test]
    public void AppendLineCharArray([Values] bool isUnsafe)
    {
        // Arrange
        char[] value = Fake.Random.Chars();
        string otherLine = Fake.Lorem.Sentence();
        string expected = new string(value) + "\n" + otherLine;
        CodeWriter writer = CompileCodeWriter(isUnsafe, "new CodeWriter().AppendLine(new char[] { }).Append(\"\").ToString();");

        // Act
        CodeWriter returnedValue = writer.AppendLine(value).Append(otherLine);

        // Assert
        AssertWriter(expected, writer, returnedValue);
    }

    [Test]
    public void AppendLineCharArraySpan([Values] bool isUnsafe)
    {
        // Arrange
        char[] value = Fake.Random.Chars(count: 32);
        string otherLine = Fake.Lorem.Sentence();
        int start = Fake.Random.Int(2, 7);
        int count = Fake.Random.Int(3, 5);
        string expected = value.AsSpan(start, count).ToString() + "\n" + otherLine;
        CodeWriter writer = CompileCodeWriter(isUnsafe, "new CodeWriter().AppendLine(new char[] { }, 0, 0).Append(\"\").ToString();");

        // Act
        CodeWriter returnedValue = writer.AppendLine(value, start, count).Append(otherLine);

        // Assert
        AssertWriter(expected, writer, returnedValue);
    }
}