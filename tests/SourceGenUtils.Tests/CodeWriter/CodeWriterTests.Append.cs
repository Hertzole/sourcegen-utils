using System;
using System.Reflection;
using Hertzole.SourceGenUtils;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

internal partial class CodeWriterTests
{
    [Test]
    [TestCaseSource(nameof(AppendCases))]
    public void Append<T>(T value)
    {
        CreateAppendTest(AppendType.Append, value);
    }

    [Test]
    [TestCaseSource(nameof(AppendFormatCases))]
    public void AppendFormat<T>(T value) where T : IFormattable
    {
        CreateAppendFormatTest(AppendType.Append, value);
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
    public void AppendCharRepeat()
    {
        // Arrange
        char value = Fake.Random.Char();
        int repeatCount = Fake.Random.Int(5, 10);
        string expected = new string(value, repeatCount);
        object writerInstance = CompileCodeWriter("CodeWriter.Append(char, int)", "CodeWriter.ToString()");
        MethodInfo appendMethod = GetMethod(writerInstance.GetType(), "Append", BindingFlags.Public | BindingFlags.Instance, typeof(char), typeof(int));

        // Act
        object? returnedValue = appendMethod.InvokeInstance(writerInstance, value, repeatCount);

        // Assert
        AssertWriter(expected, writerInstance, returnedValue);
    }

    [Test]
    public void AppendCharArray()
    {
        // Arrange
        char[] value = Fake.Random.Chars();
        string expected = new string(value);
        object writer = CompileCodeWriter("Append(char[])", "ToString()");
        MethodInfo appendMethod = GetMethod(writer.GetType(), "Append", BindingFlags.Public | BindingFlags.Instance, typeof(char[]));

        // Act
        object? returnedValue = appendMethod.InvokeInstance(writer, value);

        // Assert
        AssertWriter(expected, writer, returnedValue);
    }

    [Test]
    public void AppendCharArraySpan()
    {
        // Arrange
        char[] value = Fake.Random.Chars(count: 32);
        int start = Fake.Random.Int(2, 7);
        int count = Fake.Random.Int(3, 5);
        string expected = value.AsSpan(start, count).ToString();
        object writer = CompileCodeWriter("Append(char[], int, int)", "ToString()");
        MethodInfo appendMethod = GetMethod(writer.GetType(), "Append", BindingFlags.Public | BindingFlags.Instance, typeof(char[]), typeof(int), typeof(int));

        // Act
        object? returnedValue = appendMethod.InvokeInstance(writer, value, start, count);

        // Assert
        AssertWriter(expected, writer, returnedValue);
    }
}