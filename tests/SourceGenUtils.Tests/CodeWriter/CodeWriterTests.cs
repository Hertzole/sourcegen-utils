using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

internal partial class CodeWriterTests : GeneratorTests
{
    public static IEnumerable AppendCases
    {
        get
        {
            yield return new TestCaseData("Test message", false).SetName("string");
            yield return new TestCaseData("Test message", true).SetName("string (Unsafe)");
            yield return new TestCaseData("Test message".AsMemory(), false).SetName("ReadOnlyMemory<char>");
            yield return new TestCaseData("Test message".AsMemory(), true).SetName("ReadOnlyMemory<char> (Unsafe)");
            yield return new TestCaseData('a', false).SetName("char");
            yield return new TestCaseData('a', true).SetName("char (Unsafe)");
            yield return new TestCaseData(byte.MaxValue, false).SetName("byte");
            yield return new TestCaseData(byte.MaxValue, true).SetName("byte (Unsafe)");
            yield return new TestCaseData(sbyte.MinValue, false).SetName("sbyte");
            yield return new TestCaseData(sbyte.MinValue, true).SetName("sbyte (Unsafe)");
            yield return new TestCaseData(short.MinValue, false).SetName("short");
            yield return new TestCaseData(short.MinValue, true).SetName("short (Unsafe)");
            yield return new TestCaseData(ushort.MaxValue, false).SetName("ushort");
            yield return new TestCaseData(ushort.MaxValue, true).SetName("ushort (Unsafe)");
            yield return new TestCaseData(int.MinValue, false).SetName("int");
            yield return new TestCaseData(int.MinValue, true).SetName("int (Unsafe)");
            yield return new TestCaseData(uint.MaxValue, false).SetName("uint");
            yield return new TestCaseData(uint.MaxValue, true).SetName("uint (Unsafe)");
            yield return new TestCaseData(long.MinValue, false).SetName("long");
            yield return new TestCaseData(long.MinValue, true).SetName("long (Unsafe)");
            yield return new TestCaseData(ulong.MaxValue, false).SetName("ulong");
            yield return new TestCaseData(ulong.MaxValue, true).SetName("ulong (Unsafe)");
            yield return new TestCaseData(1.1f, false).SetName("float");
            yield return new TestCaseData(1.1f, true).SetName("float (Unsafe)");
            yield return new TestCaseData(1.1d, false).SetName("double");
            yield return new TestCaseData(1.1d, true).SetName("double (Unsafe)");
            yield return new TestCaseData(1.1m, false).SetName("decimal");
            yield return new TestCaseData(1.1m, true).SetName("decimal (Unsafe)");
            yield return new TestCaseData(false, false).SetName("bool");
            yield return new TestCaseData(false, true).SetName("bool (Unsafe)");
            yield return new TestCaseData(new TestObj(), false).SetName("object");
            yield return new TestCaseData(new TestObj(), true).SetName("object (Unsafe)");
        }
    }

    public static IEnumerable AppendFormatCases
    {
        get
        {
            yield return new TestCaseData(byte.MaxValue, false).SetName("byte");
            yield return new TestCaseData(byte.MaxValue, true).SetName("byte (unsafe)");
            yield return new TestCaseData(sbyte.MinValue, false).SetName("sbyte");
            yield return new TestCaseData(sbyte.MinValue, true).SetName("sbyte (unsafe)");
            yield return new TestCaseData(short.MaxValue, false).SetName("short");
            yield return new TestCaseData(short.MaxValue, true).SetName("short (unsafe)");
            yield return new TestCaseData(ushort.MinValue, false).SetName("ushort");
            yield return new TestCaseData(ushort.MinValue, true).SetName("ushort (unsafe)");
            yield return new TestCaseData(int.MaxValue, false).SetName("int");
            yield return new TestCaseData(int.MaxValue, true).SetName("int (unsafe)");
            yield return new TestCaseData(uint.MinValue, false).SetName("uint");
            yield return new TestCaseData(uint.MinValue, true).SetName("uint (unsafe)");
            yield return new TestCaseData(long.MaxValue, false).SetName("long");
            yield return new TestCaseData(long.MaxValue, true).SetName("long (unsafe)");
            yield return new TestCaseData(ulong.MinValue, false).SetName("ulong");
            yield return new TestCaseData(ulong.MinValue, true).SetName("ulong (unsafe)");
            yield return new TestCaseData(1.1f, false).SetName("float");
            yield return new TestCaseData(1.1f, true).SetName("float (Unsafe)");
            yield return new TestCaseData(1.1d, false).SetName("double");
            yield return new TestCaseData(1.1d, true).SetName("double (Unsafe)");
            yield return new TestCaseData(1.1m, false).SetName("decimal");
            yield return new TestCaseData(1.1m, true).SetName("decimal (Unsafe)");
        }
    }

    private const string TEST_NAMESPACE = "My.Testing.Namespace";

    public static IEnumerable AppendSymbolCases
    {
        get
        {
            yield return new TestCaseData(BuildTypeSource("class A"), true, true)
                         .SetName("Class without namespace (Partial, Include namespace)")
                         .Returns(BuildSymbolDeclaration("partial class A"));

            yield return new TestCaseData(BuildTypeSource("class A"), false, true)
                         .SetName("Class without namespace (Include namespace)")
                         .Returns(BuildSymbolDeclaration("class A"));

            yield return new TestCaseData(BuildTypeSource("class A"), false, false)
                         .SetName("Class without namespace ()")
                         .Returns(BuildSymbolDeclaration("class A"));

            yield return new TestCaseData(BuildTypeSource("class A", TEST_NAMESPACE), true, true)
                         .SetName("Class with namespace (Partial, Include namespace)")
                         .Returns(BuildSymbolDeclaration("partial class A", TEST_NAMESPACE));

            yield return new TestCaseData(BuildTypeSource("class A", TEST_NAMESPACE), false, true)
                         .SetName("Class with namespace (Include namespace)")
                         .Returns(BuildSymbolDeclaration("class A", TEST_NAMESPACE));

            yield return new TestCaseData(BuildTypeSource("class A", TEST_NAMESPACE), false, false)
                         .SetName("Class with namespace ()")
                         .Returns(BuildSymbolDeclaration("class A"));

            yield return new TestCaseData(BuildTypeSource("struct A"), true, true)
                         .SetName("Struct without namespace (Partial, Include namespace)")
                         .Returns(BuildSymbolDeclaration("partial struct A"));

            yield return new TestCaseData(BuildTypeSource("struct A"), false, true)
                         .SetName("Struct without namespace (Include namespace)")
                         .Returns(BuildSymbolDeclaration("struct A"));

            yield return new TestCaseData(BuildTypeSource("struct A"), false, false)
                         .SetName("Struct without namespace ()")
                         .Returns(BuildSymbolDeclaration("struct A"));

            yield return new TestCaseData(BuildTypeSource("struct A", TEST_NAMESPACE), true, true)
                         .SetName("Struct with namespace (Partial, Include namespace)")
                         .Returns(BuildSymbolDeclaration("partial struct A", TEST_NAMESPACE));

            yield return new TestCaseData(BuildTypeSource("struct A", TEST_NAMESPACE), false, true)
                         .SetName("Struct with namespace (Include namespace)")
                         .Returns(BuildSymbolDeclaration("struct A", TEST_NAMESPACE));

            yield return new TestCaseData(BuildTypeSource("struct A", TEST_NAMESPACE), false, false)
                         .SetName("Struct with namespace ()")
                         .Returns(BuildSymbolDeclaration("struct A"));
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

    private static void CreateAppendTest<T>(AppendType appendType, bool isUnsafe, T value)
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
        object writer = CompileCodeWriter(isUnsafe, $"{appendMethod}({GetTypesString(type)})", "ToString()", appendLineMethodName);
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

    private static void CreateAppendFormatTest<T>(AppendType appendType, bool isUnsafe, T value) where T : IFormattable
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
        object writer = CompileCodeWriter(isUnsafe, $"{appendMethod}({GetTypesString(type)}, string, System.IFormatProvider)", "ToString()");
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

    private static object CompileCodeWriter(bool isUnsafe, params string[] calledMethods)
    {
        string[] calledWithConstructor = ["CodeWriter.CodeWriter()", .. calledMethods];
        Type type = isUnsafe ? CompileUnsafeGeneratedType("CodeWriter", calledWithConstructor) : CompileGeneratedType("CodeWriter", calledWithConstructor);
        object? writer = Activator.CreateInstance(type);

        Assert.That(writer, Is.Not.Null, $"Couldn't create writer from type {type.FullName}");

        return writer!;
    }

    private static string BuildTypeSource(string signature, string? typeNamespace = null)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("using System;");
        sb.AppendLine();

        int indent = 0;

        if (!string.IsNullOrWhiteSpace(typeNamespace))
        {
            sb.Append("namespace ");
            sb.AppendLine(typeNamespace);
            sb.AppendLine("{");
            indent++;
        }

        sb.Append(' ', indent * 4);
        sb.AppendLine(signature);

        sb.Append(' ', indent * 4);
        sb.AppendLine("{ }");

        if (!string.IsNullOrWhiteSpace(typeNamespace))
        {
            indent--;

            sb.Append(' ', indent * 4);
            sb.AppendLine("}");
        }

        return sb.ToString().Trim();
    }

    private static string BuildSymbolDeclaration(string signature, string? typeNamespace = null)
    {
        StringBuilder sb = new StringBuilder();

        int indent = 0;

        if (!string.IsNullOrWhiteSpace(typeNamespace))
        {
            sb.Append("namespace ");
            sb.AppendLine(typeNamespace);
            sb.AppendLine("{");
            indent++;
        }

        sb.Append(' ', indent * 4);
        sb.AppendLine(signature);

        if (!string.IsNullOrWhiteSpace(typeNamespace))
        {
            indent--;

            sb.Append(' ', indent * 4);
            sb.AppendLine("}");
        }

        return sb.ToString().Trim();
    }
}