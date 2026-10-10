using System;
using System.Collections;
using System.Text;
using Hertzole.SourceGen.Wrappers;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

internal partial class CodeWriterTests
{
    public static IEnumerable AppendIndentedSourceCases
    {
        get
        {
            yield return new TestCaseData(INDENTED_SOURCE).SetName("Normal");
            yield return new TestCaseData(MessUpSource(INDENTED_SOURCE, true, false)).SetName("Windows new-line");
            yield return new TestCaseData(MessUpSource(INDENTED_SOURCE, false, true)).SetName("With tab");
            yield return new TestCaseData(MessUpSource(INDENTED_SOURCE, true, true)).SetName("Windows new-line with tab");
        }
    }

    [Test]
    [TestCaseSource(nameof(AppendCases))]
    public void Append<T>(T value, bool isUnsafe)
    {
        CreateAppendTest(AppendType.Append, isUnsafe, value);
    }

    [Test]
    [TestCaseSource(nameof(AppendFormatCases))]
    public void AppendFormat<T>(T value, bool isUnsafe) where T : IFormattable
    {
        CreateAppendFormatTest(AppendType.Append, isUnsafe, value);
    }

    [Test]
    public void AppendArrayBuilder()
    {
        // Arrange
        string message = Fake.Lorem.Sentence();
        CodeWriter writer = CompileCodeWriter(false,
            "var builder = new ArrayBuilder<char>(); builder.Add('a'); new CodeWriter().Append(builder).ToString();");

        ArrayBuilder<char> arrayBuilder = new ArrayBuilder<char>(writer.Type.Assembly);

        // Act
        // Because we can't pass ReadOnlySpan in args, add each letter instead.
        for (int i = 0; i < message.Length; i++)
        {
            arrayBuilder.Add(message[i]);
        }

        CodeWriter returnedValue = writer.Append(arrayBuilder);

        // Assert
        AssertWriter(message, writer, returnedValue);
    }

    [Test]
    public void AppendCharRepeat([Values] bool isUnsafe)
    {
        // Arrange
        char value = Fake.Random.Char();
        int repeatCount = Fake.Random.Int(5, 10);
        string expected = new string(value, repeatCount);
        CodeWriter writer = CompileCodeWriter(isUnsafe, "new CodeWriter().Append('a', 1).ToString();");

        // Act
        CodeWriter returnedValue = writer.Append(value, repeatCount);

        // Assert
        AssertWriter(expected, writer, returnedValue);
    }

    [Test]
    public void AppendCharArray([Values] bool isUnsafe)
    {
        // Arrange
        char[] value = Fake.Random.Chars();
        string expected = new string(value);
        CodeWriter writer = CompileCodeWriter(isUnsafe, "new CodeWriter().Append(new char[] { }).ToString();");

        // Act
        CodeWriter returnedValue = writer.Append(value);

        // Assert
        AssertWriter(expected, writer, returnedValue);
    }

    [Test]
    public void AppendCharArraySpan([Values] bool isUnsafe)
    {
        // Arrange
        char[] value = Fake.Random.Chars(count: 32);
        int start = Fake.Random.Int(2, 7);
        int count = Fake.Random.Int(3, 5);
        string expected = value.AsSpan(start, count).ToString();
        CodeWriter writer = CompileCodeWriter(isUnsafe, "new CodeWriter().Append(new char[] { }, 0, 0).ToString();");

        // Act
        CodeWriter returnedValue = writer.Append(value, start, count);

        // Assert
        AssertWriter(expected, writer, returnedValue);
    }

    private const string EXPECTED_INDENTED_SOURCE = """
                                                    public void Method(bool value)
                                                    {
                                                        // This is a comment
                                                        if (value)
                                                        {
                                                            // Do thing
                                                        }

                                                        int amount = 69;
                                                        for (int i = 0; i < amount; i++)
                                                        {
                                                            if (amount % 2 == 0)
                                                            {
                                                                // Do other thing
                                                            }
                                                        }
                                                    }
                                                    """;

    private const string INDENTED_SOURCE = """
                                           // This is a comment
                                           if (value)
                                           {
                                               // Do thing
                                           }

                                           int amount = 69;
                                           for (int i = 0; i < amount; i++)
                                           {
                                               if (amount % 2 == 0)
                                               {
                                                   // Do other thing
                                               }
                                           }
                                           """;

    [Test]
    [TestCaseSource(nameof(AppendIndentedSourceCases))]
    public void AppendIndentedSource(string value)
    {
        // Arrange
        CodeWriter writer = CompileCodeWriter(false,
            "var writer = new CodeWriter(); writer.Indent = 1; writer.AppendLine(\"\"); writer.AppendIndentedSource(\"\"); writer.Indent = 0; writer.ToString();");

        // Act
        writer.AppendLine("public void Method(bool value)");
        writer.AppendLine("{");
        writer.Indent = 1;
        CodeWriter returned = writer.AppendIndentedSource(value);
        writer.Indent = 0;
        writer.AppendLine("}");

        // Assert
        Assert.That(EXPECTED_INDENTED_SOURCE, Is.EqualTo(writer.ToString()));
        Assert.That(returned.Instance, Is.SameAs(writer.Instance));
    }

    [Test]
    [TestCaseSource(nameof(AppendSymbolCases))]
    public string AppendSymbol(string source, bool isPartial, bool includeNamespace)
    {
        // Arrange
        INamedTypeSymbol symbol = RoslynHelper.CompileTypeToSymbol(source);
        CodeWriter writer = CompileCodeWriter(false,
            "new CodeWriter().Append((Microsoft.CodeAnalysis.ITypeSymbol)null!, true, true).ToString();");

        // Act
        writer.Append(symbol, isPartial, includeNamespace);
        return writer.ToString();
    }

    private static string MessUpSource(string value, bool replaceNewLines, bool replaceTabs)
    {
        StringBuilder sb = new StringBuilder(value);

        if (replaceNewLines)
        {
            sb.Replace("\n", "\r\n");
        }

        if (replaceTabs)
        {
            sb.Replace("    ", "\t");
        }

        return sb.ToString();
    }
}