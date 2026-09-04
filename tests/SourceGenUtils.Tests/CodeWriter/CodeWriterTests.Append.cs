using System;
using System.Collections;
using System.Reflection;
using System.Text;
using Hertzole.SourceGenUtils;
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
        Type writerType = CompileGeneratedType("CodeWriter", "CodeWriter.CodeWriter()", $"CodeWriter.Append({Generator.NAMESPACE}.ArrayBuilder<char>)",
            "CodeWriter.ToString()", "ArrayBuilder.ArrayBuilder()", "ArrayBuilder.Add(T)");

        object writerInstance = CreateInstance(writerType);
        Type arrayBuilderType = writerType.Assembly.GetType($"{Generator.NAMESPACE}.ArrayBuilder`1", true)!.MakeGenericType(typeof(char));
        object arrayBuilderInstance = CreateInstance(arrayBuilderType);
        MethodInfo appendMethod = GetMethod(writerType, "Append", BindingFlags.Public | BindingFlags.Instance, arrayBuilderType);
        MethodInfo addRangeMethod = GetMethod(arrayBuilderType, "Add", BindingFlags.Public | BindingFlags.Instance);

        // Act
        // Because we can't pass ReadOnlySpan in args, add each letter instead.
        for (int i = 0; i < message.Length; i++)
        {
            addRangeMethod.InvokeInstance(arrayBuilderInstance, message[i]);
        }

        object? value = appendMethod.InvokeInstance(writerInstance, arrayBuilderInstance);

        // Assert
        AssertWriter(message, writerInstance, value);
    }

    [Test]
    public void AppendCharRepeat([Values] bool isUnsafe)
    {
        // Arrange
        char value = Fake.Random.Char();
        int repeatCount = Fake.Random.Int(5, 10);
        string expected = new string(value, repeatCount);
        object writerInstance = CompileCodeWriter(isUnsafe, "CodeWriter.Append(char, int)", "CodeWriter.ToString()");
        MethodInfo appendMethod = GetMethod(writerInstance.GetType(), "Append", BindingFlags.Public | BindingFlags.Instance, typeof(char), typeof(int));

        // Act
        object? returnedValue = appendMethod.InvokeInstance(writerInstance, value, repeatCount);

        // Assert
        AssertWriter(expected, writerInstance, returnedValue);
    }

    [Test]
    public void AppendCharArray([Values] bool isUnsafe)
    {
        // Arrange
        char[] value = Fake.Random.Chars();
        string expected = new string(value);
        object writer = CompileCodeWriter(isUnsafe, "Append(char[])", "ToString()");
        MethodInfo appendMethod = GetMethod(writer.GetType(), "Append", BindingFlags.Public | BindingFlags.Instance, typeof(char[]));

        // Act
        object? returnedValue = appendMethod.InvokeInstance(writer, value);

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
        object writer = CompileCodeWriter(isUnsafe, "Append(char[], int, int)", "ToString()");
        MethodInfo appendMethod = GetMethod(writer.GetType(), "Append", BindingFlags.Public | BindingFlags.Instance, typeof(char[]), typeof(int), typeof(int));

        // Act
        object? returnedValue = appendMethod.InvokeInstance(writer, value, start, count);

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
        object writer = CompileCodeWriter(false, "AppendIndentedSource(string)", "AppendLine(string)", "ToString()");
        MethodInfo appendLine = GetMethod(writer.GetType(), "AppendLine", BindingFlags.Public | BindingFlags.Instance, typeof(string));
        MethodInfo appendMethod = GetMethod(writer.GetType(), "AppendIndentedSource", BindingFlags.Public | BindingFlags.Instance, typeof(string));
        PropertyInfo indent = GetProperty(writer.GetType(), "Indent", BindingFlags.Public | BindingFlags.Instance);

        // Act
        appendLine.InvokeInstance(writer, "public void Method(bool value)");
        appendLine.InvokeInstance(writer, "{");
        indent.SetValue(writer, 1);
        object? returned = appendMethod.InvokeInstance(writer, value);
        indent.SetValue(writer, 0);
        appendLine.InvokeInstance(writer, "}");

        // Assert
        Assert.That(EXPECTED_INDENTED_SOURCE, Is.EqualTo(writer.ToString()));
        Assert.That(returned, Is.SameAs(writer));
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