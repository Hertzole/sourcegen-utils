using System;
using System.Reflection;
using Hertzole.SourceGenUtils;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

internal partial class CodeWriterTests
{
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
}