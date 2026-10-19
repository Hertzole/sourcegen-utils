using System;
using System.Collections;
using System.Globalization;
using System.Text;
using Hertzole.SourceGen.Wrappers;
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

            yield return new TestCaseData(BuildTypeSource("interface A"), true, true)
                         .SetName("Interface without namespace (Partial, Include namespace)")
                         .Returns(BuildSymbolDeclaration("partial interface A"));

            yield return new TestCaseData(BuildTypeSource("interface A"), false, true)
                         .SetName("Interface without namespace (Include namespace)")
                         .Returns(BuildSymbolDeclaration("interface A"));

            yield return new TestCaseData(BuildTypeSource("interface A"), false, false)
                         .SetName("Interface without namespace ()")
                         .Returns(BuildSymbolDeclaration("interface A"));

            yield return new TestCaseData(BuildTypeSource("interface A", TEST_NAMESPACE), true, true)
                         .SetName("Interface with namespace (Partial, Include namespace)")
                         .Returns(BuildSymbolDeclaration("partial interface A", TEST_NAMESPACE));

            yield return new TestCaseData(BuildTypeSource("interface A", TEST_NAMESPACE), false, true)
                         .SetName("Interface with namespace (Include namespace)")
                         .Returns(BuildSymbolDeclaration("interface A", TEST_NAMESPACE));

            yield return new TestCaseData(BuildTypeSource("interface A", TEST_NAMESPACE), false, false)
                         .SetName("Interface with namespace ()")
                         .Returns(BuildSymbolDeclaration("interface A"));
        }
    }

    private const string TEST_NAMESPACE = "My.Testing.Namespace";

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

    [Test]
    public void WithBlock([Values] bool newLineOnDispose)
    {
        // Arrange
        CodeWriter writer = CompileCodeWriter(false, "var writer = new CodeWriter().AppendLine(\"\").Append(\"\"); writer.WithBlock(); writer.ToString();");
        // Use \n because code writer appends \n instead of using environment new line.
        StringBuilder builder = new StringBuilder()
                                .Append("{\n")
                                .Append("    Inner\n")
                                .Append("}\n");

        if (newLineOnDispose)
        {
            builder.Append('\n');
        }

        builder.Append("Outer");
        string expected = builder.ToString();

        // Act
        using (writer.WithBlock(newLineOnDispose))
        {
            writer.AppendLine("Inner");
        }

        writer.Append("Outer");

        // Assert
        Assert.That(writer.ToString(), Is.EqualTo(expected));
    }

    [Test]
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(4)]
    public void WithIndent(int indent)
    {
        // Arrange
        const int start_indent = 2;
        const int indent_size = 4;
        string message = Fake.Lorem.Sentence();
        string message2 = Fake.Lorem.Sentence();
        CodeWriter writer = CompileCodeWriter(false,
            "var writer = new CodeWriter().AppendLine(\"\").Append(\"\"); writer.Indent = 0; writer.WithIndent(0); writer.ToString();");

        StringBuilder builder = new StringBuilder()
                                .Append(' ', start_indent * indent_size).Append(message).Append('\n')
                                .Append(' ', indent * indent_size).Append(message2); // indent by 4 spaces per indent

        string expected = builder.ToString();

        // Act
        writer.Indent = start_indent;
        writer.AppendLine(message);
        using (writer.WithIndent(indent))
        {
            writer.Append(message2);
        }

        // Assert
        Assert.That(writer.ToString(), Is.EqualTo(expected));
    }

    [Test]
    [TestCase("#if DEBUG")]
    [TestCase("if DEBUG")]
    [TestCase(" DEBUG")]
    [TestCase("DEBUG")]
    public void WithCondition(string condition)
    {
        // Arrange
        string message = Fake.Lorem.Sentence();
        CodeWriter writer = CompileCodeWriter(false, "var writer = new CodeWriter().AppendLine(\"\"); writer.WithCondition(\"\"); writer.ToString();");
        string expected = $"{message}\n#if DEBUG\n{message}\n#endif";

        // Act
        writer.AppendLine(message);
        using (writer.WithCondition(condition))
        {
            writer.AppendLine(message);
        }

        // Assert
        Assert.That(writer.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public void ToString_NothingWritten_IsEmpty()
    {
        // Arrange
        CodeWriter writer = CompileCodeWriter(false, "new CodeWriter().ToString();");

        // Act
        string result = writer.ToString();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Clear()
    {
        // Arrange
        CodeWriter writer = CompileCodeWriter(false,
            "var writer = new CodeWriter().AppendLine(\"\").AppendNamespace(\"\").AppendNullable().Clear(); Use(writer.Indent); writer.ToString();");

        // Act
        writer.Indent = 10;
        writer.AppendLine("// Comment");
        writer.AppendNullable();
        writer.AppendNamespace("Namespace");
        writer.Clear();

        // Assert
        Assert.That(writer.Indent, Is.EqualTo(0));
        Assert.That(writer.builder.Length, Is.EqualTo(0));
        Assert.That(writer.ToString(), Is.Empty);
    }

    [Test]
    public void IndentIsWrittenOnFirstCall()
    {
        // Arrange
        CodeWriter writer = CompileCodeWriter(false, "var writer = new CodeWriter().AppendLine(\"\"); writer.Indent = 0; writer.Clear(); writer.ToString();");
        const string expected = "    test";

        // Act
        writer.Indent = 1;
        writer.AppendLine("test");
        string result1 = writer.ToString();
        writer.Clear();

        writer.Indent = 1;
        writer.AppendLine("test");
        string result2 = writer.ToString();

        // Assert
        Assert.That(result1, Is.EqualTo(expected));
        Assert.That(result2, Is.EqualTo(expected));
    }

    private static void CreateAppendTest<T>(AppendType appendType, bool isUnsafe, T value)
    {
        Type type = typeof(T);

        if (type == typeof(TestObj))
        {
            // To test normal object ToString.
            type = typeof(object);
        }

        string usage = appendType == AppendType.Append
            ? $"new CodeWriter().Append(default({GetTypesString(type)})).ToString();"
            : $"new CodeWriter().AppendLine(string.Empty).AppendLine(default({GetTypesString(type)})).ToString();";

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

        // Arrange
        CodeWriter writer = CompileCodeWriter(isUnsafe, usage);

        // Act
        if (line1 != null)
        {
            writer.AppendLine(line1);
        }

        CodeWriter returnedValue = appendType == AppendType.Append
            ? writer.Append((dynamic) value!)
            : writer.AppendLine((dynamic) value!);

        // Assert
        AssertWriter(expectedMessage, writer, returnedValue);
    }

    private static void CreateAppendFormatTest<T>(AppendType appendType, bool isUnsafe, T value) where T : IFormattable
    {
        string appendMethod = appendType == AppendType.Append ? "Append" : "AppendLine";

        // Arrange
        Type type = typeof(T);
        CultureInfo culture = Fake.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        const string format = "P1";
        string expected = value.ToString(format, culture);

        string usage = $"new CodeWriter().{appendMethod}(default({GetTypesString(type)}), string.Empty, null).ToString();";
        CodeWriter writer = CompileCodeWriter(isUnsafe, usage);

        // Act
        CodeWriter returnedValue = appendType == AppendType.Append
            ? writer.Append((dynamic) value, format, culture)
            : writer.AppendLine((dynamic) value, format, culture);

        // Assert
        AssertWriter(expected, writer, returnedValue);
    }

    private static void AssertWriter(string expectedMessage, CodeWriter writer, CodeWriter returnedValue)
    {
        Assert.That(writer.ToString(), Is.EqualTo(expectedMessage));
        Assert.That(returnedValue, Is.Not.Null);
        Assert.That(returnedValue.Instance, Is.SameAs(writer.Instance));
    }

    private static CodeWriter CompileCodeWriter(bool isUnsafe, string usage)
    {
        return new CodeWriter(AssemblyBuilder.CompileGeneratedTypeByUsing("CodeWriter", usage, isUnsafe));
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