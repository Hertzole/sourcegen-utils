using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

internal partial class CodeWriterTests : GeneratorTests
{
    public static IEnumerable AppendCases
    {
        get
        {
            yield return new TestCaseData("Test message");
            yield return new TestCaseData("Test message".AsMemory());
            yield return new TestCaseData('a');
            yield return new TestCaseData(byte.MaxValue);
            yield return new TestCaseData(sbyte.MinValue);
            yield return new TestCaseData(short.MinValue);
            yield return new TestCaseData(ushort.MaxValue);
            yield return new TestCaseData(int.MinValue);
            yield return new TestCaseData(uint.MaxValue);
            yield return new TestCaseData(long.MinValue);
            yield return new TestCaseData(ulong.MaxValue);
            yield return new TestCaseData(1.1f);
            yield return new TestCaseData(1.1d);
            yield return new TestCaseData(1.1m);
            yield return new TestCaseData(false);
            yield return new TestCaseData(new TestObj());
        }
    }

    public static IEnumerable AppendFormatCases
    {
        get
        {
            yield return new TestCaseData(byte.MaxValue);
            yield return new TestCaseData(sbyte.MinValue);
            yield return new TestCaseData(short.MaxValue);
            yield return new TestCaseData(ushort.MinValue);
            yield return new TestCaseData(int.MaxValue);
            yield return new TestCaseData(uint.MinValue);
            yield return new TestCaseData(long.MaxValue);
            yield return new TestCaseData(ulong.MinValue);
            yield return new TestCaseData(1.1f);
            yield return new TestCaseData(1.1d);
            yield return new TestCaseData(1.1m);
        }
    }

    private class TestObj
    {
        /// <inheritdoc />
        public override string ToString()
        {
            return "TestObj";
        }
    }

    private enum AppendType
    {
        Append,
        AppendLine
    }

    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "CodeWriter";
    }

    private static void CreateAppendTest<T>(AppendType appendType, T value)
    {
        Type type = typeof(T);

        if (type == typeof(TestObj))
        {
            // To test normal object ToString.
            type = typeof(object);
        }

        string appendMethod = appendType switch
        {
            AppendType.Append => "Append",
            AppendType.AppendLine => "AppendLine",
            _ => throw new ArgumentOutOfRangeException(nameof(appendType), appendType, null)
        };

        string appendLineMethodName = appendType switch
        {
            AppendType.AppendLine => "AppendLine(string)",
            _ => string.Empty
        };

        // Arrange
        object writer = CompileCodeWriter($"{appendMethod}({GetTypesString(type)})", "ToString()", appendLineMethodName);
        MethodInfo targetMethod = GetMethod(writer.GetType(), appendMethod, BindingFlags.Public | BindingFlags.Instance, type);
        MethodInfo? appendLineMethod = null;
        string expectedMessage;
        string? line1 = null;

        if (appendType == AppendType.Append)
        {
            expectedMessage = value!.ToString()!;

            if (type == typeof(bool))
            {
                // For bool it expects a lowercase value.
                expectedMessage = expectedMessage.ToLowerInvariant();
            }
        }
        else
        {
            appendLineMethod = GetMethod(writer.GetType(), "AppendLine", BindingFlags.Public | BindingFlags.Instance, typeof(string));
            line1 = Fake.Lorem.Sentence();
            string valueString = value!.ToString()!;

            if (type == typeof(bool))
            {
                // For bool it expects a lowercase value.
                valueString = valueString.ToLowerInvariant();
            }

            expectedMessage = $"""
                               {line1}
                               {valueString}
                               """;
        }

        // Act
        if (appendLineMethod != null && line1 != null)
        {
            appendLineMethod.InvokeInstance(writer, line1);
        }

        object? returnedValue = targetMethod.InvokeInstance(writer, value!);

        // Assert
        AssertWriter(expectedMessage, writer, returnedValue);
    }

    private static void CreateAppendFormatTest<T>(AppendType appendType, T value) where T : IFormattable
    {
        string appendMethod = appendType switch
        {
            AppendType.Append => "Append",
            AppendType.AppendLine => "AppendLine",
            _ => throw new ArgumentOutOfRangeException(nameof(appendType), appendType, null)
        };

        // Arrange
        Type type = typeof(T);
        CultureInfo culture = Fake.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        const string format = "P1";
        string expected = value.ToString(format, culture);
        object writer = CompileCodeWriter($"{appendMethod}({GetTypesString(type)}, string, System.IFormatProvider)", "ToString()");
        MethodInfo method = GetMethod(writer.GetType(), appendMethod, BindingFlags.Public | BindingFlags.Instance, type, typeof(string),
            typeof(IFormatProvider));

        // Act
        object? returnedValue = method.InvokeInstance(writer, value, format, culture);

        // Assert
        AssertWriter(expected, writer, returnedValue);
    }

    private static void AssertWriter(string expectedMessage, object writer, object? returnedValue)
    {
        Assert.That(writer.ToString(), Is.EqualTo(expectedMessage));
        Assert.That(returnedValue, Is.Not.Null);
        Assert.That(returnedValue, Is.SameAs(writer));
        Assert.That(returnedValue!.GetType(), Is.EqualTo(writer.GetType()));
    }

    private static object CompileCodeWriter(params string[] calledMethods)
    {
        for (int i = 0; i < calledMethods.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(calledMethods[i]))
            {
                continue;
            }

            if (!calledMethods[i].StartsWith("CodeWriter"))
            {
                calledMethods[i] = "CodeWriter." + calledMethods[i];
            }
        }

        string[] calledWithConstructor = new string[calledMethods.Length + 1];
        calledMethods.CopyTo(calledWithConstructor, 0);
        calledWithConstructor[calledMethods.Length] = "CodeWriter.CodeWriter()";
        Type type = CompileGeneratedType("CodeWriter", calledWithConstructor);
        object? writer = Activator.CreateInstance(type);

        Assert.That(writer, Is.Not.Null, $"Couldn't create writer from type {type.FullName}");

        return writer!;
    }
}