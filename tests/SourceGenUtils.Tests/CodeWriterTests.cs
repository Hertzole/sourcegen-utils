using Hertzole.SourceGenUtils;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

internal class CodeWriterTests : GeneratorTests
{
    private const string NEW_LINE_FORMATTABLE = "return AppendLine(value.ToString(\"G\", global::System.Globalization.CultureInfo.InvariantCulture));";

    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "CodeWriter";
    }

    /// <inheritdoc />
    protected override string GetTypeOutline()
    {
        return """
               internal sealed partial class CodeWriter : global::System.IDisposable
               {
               }
               """;
    }

    /// <inheritdoc />
    protected override string[] GetShellMethods()
    {
        const string format_provider = "System.IFormatProvider?";
        const string format_args = $"string, {format_provider}";

        return
        [
            "CodeWriter()",
            "AppendNullable()",
            "Append(string?)",
            "Append(System.ReadOnlySpan<char>)",
            "Append(System.ReadOnlyMemory<char>)",
            "Append(char)",
            "Append(char, int)",
            "Append(char[])",
            "Append(char[], int, int)",
            "Append(byte)",
            $"Append(byte, {format_args})",
            "Append(sbyte)",
            $"Append(sbyte, {format_args})",
            "Append(short)",
            $"Append(short, {format_args})",
            "Append(ushort)",
            $"Append(ushort, {format_args})",
            "Append(int)",
            $"Append(int, {format_args})",
            "Append(uint)",
            $"Append(uint, {format_args})",
            "Append(long)",
            $"Append(long, {format_args})",
            "Append(ulong)",
            $"Append(ulong, {format_args})",
            "Append(float)",
            $"Append(float, {format_args})",
            "Append(double)",
            $"Append(double, {format_args})",
            "Append(decimal)",
            $"Append(decimal, {format_args})",
            "Append(bool)",
            "Append(object)",
            "AppendLine()",
            "AppendLine(string?)",
            "AppendLine(System.ReadOnlySpan<char>)",
            "AppendLine(System.ReadOnlyMemory<char>)",
            "AppendLine(char)",
            "AppendLine(char, int)",
            "AppendLine(char[], int, int)",
            "AppendLine(byte)",
            $"AppendLine(byte, {format_args})",
            "AppendLine(sbyte)",
            $"AppendLine(sbyte, {format_args})",
            "AppendLine(short)",
            $"AppendLine(short, {format_args})",
            "AppendLine(ushort)",
            $"AppendLine(ushort, {format_args})",
            "AppendLine(int)",
            $"AppendLine(int, {format_args})",
            "AppendLine(uint)",
            $"AppendLine(uint, {format_args})",
            "AppendLine(long)",
            $"AppendLine(long, {format_args})",
            "AppendLine(ulong)",
            $"AppendLine(ulong, {format_args})",
            "AppendLine(float)",
            $"AppendLine(float, {format_args})",
            "AppendLine(double)",
            $"AppendLine(double, {format_args})",
            "AppendLine(decimal)",
            $"AppendLine(decimal, {format_args})",
            "AppendLine(bool)",
            "AppendLine(object)",
            "AppendNamespace(Microsoft.CodeAnalysis.INamespaceSymbol?)",
            "AppendNamespace(string)",
            "AppendGeneratedCodeAttribute(string, string)",
            "AppendEmbeddedAttribute()",
            "AppendExcludeFromCodeCoverageAttribute()",
            "AppendConditionalSymbol(string?)",
            "AppendPreprocessorSymbol(string?)",
            "Clear()",
            "ToString()",
            "Dispose()",
            "WithBlock()",
            "WithIndent(int)",
            "WithCondition(string?)"
        ];
    }

    [Test]
    public void Call_AppendString()
    {
        string[] expectedMethods =
        [
            "CodeWriter.Append(string?)",
            "CodeWriter.WriteIndentIfNeeded()",
            "CodeWriter.ThrowIfDisposed()",
            "CodeWriter.CodeWriter()",
            "CodeWriter.Dispose()"
        ];

        AssertCallingMethodCreatesMethods(writer => { writer.AppendLine("new CodeWriter().Append(\"hello\");"); }, expectedMethods);
    }

    [Test]
    public void Call_AppendLine()
    {
        string[] expectedMethods =
        [
            "CodeWriter.AppendLine()",
            "CodeWriter.ThrowIfDisposed()",
            "CodeWriter.CodeWriter()",
            "CodeWriter.Dispose()"
        ];

        AssertCallingMethodCreatesMethods(writer => { writer.AppendLine("new CodeWriter().AppendLine();"); }, expectedMethods);
    }

    [Test]
    public void Call_AppendNullable()
    {
        string[] expectedMethods =
        [
            "CodeWriter.AppendNullable()",
            "CodeWriter.ThrowIfDisposed()",
            "CodeWriter.CodeWriter()",
            "CodeWriter.Dispose()"
        ];

        AssertCallingMethodCreatesMethods(writer => { writer.AppendLine("new CodeWriter().AppendNullable();"); }, expectedMethods);
    }

    [Test]
    public void Call_AppendNamespace()
    {
        string[] expectedMethods =
        [
            "CodeWriter.AppendNamespace(string)",
            "CodeWriter.ThrowIfDisposed()",
            "CodeWriter.CodeWriter()",
            "CodeWriter.Dispose()"
        ];

        AssertCallingMethodCreatesMethods(writer => { writer.AppendLine("new CodeWriter().AppendNamespace(\"Test\");"); }, expectedMethods);
    }

    [Test]
    public void Call_ToString()
    {
        string[] expectedMethods =
        [
            "CodeWriter.Append(string?)",
            "CodeWriter.ToString()",
            "CodeWriter.WriteIndentIfNeeded()",
            "CodeWriter.ThrowIfDisposed()",
            "CodeWriter.CodeWriter()",
            "CodeWriter.Dispose()"
        ];

        AssertCallingMethodCreatesMethods(writer =>
        {
            writer.AppendLine("var cw = new CodeWriter();");
            writer.AppendLine("cw.Append(\"hello\");");
            writer.AppendLine("_ = cw.ToString();");
        }, expectedMethods);
    }

    [Test]
    public void Call_Clear()
    {
        string[] expectedMethods =
        [
            "CodeWriter.Append(string?)",
            "CodeWriter.Clear()",
            "CodeWriter.WriteIndentIfNeeded()",
            "CodeWriter.ThrowIfDisposed()",
            "CodeWriter.CodeWriter()",
            "CodeWriter.Dispose()"
        ];

        AssertCallingMethodCreatesMethods(writer =>
        {
            writer.AppendLine("var cw = new CodeWriter();");
            writer.AppendLine("cw.Append(\"hello\");");
            writer.AppendLine("cw.Clear();");
        }, expectedMethods);
    }

    [Test]
    public void AppendNullable_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendNullable()");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendNullable()
                                {
                                    ThrowIfDisposed();
                                    builder.Append("#nullable enable");
                                    builder.Append('\n');
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendNullable_WithToString_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendNullable()", "CodeWriter.ToString()");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendNullable()
                                {
                                    ThrowIfDisposed();
                                    builder.Append("#nullable enable");
                                    builder.Append('\n');
                                    isNullable = true;
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Append_String_Content()
    {
        string content = GetMethodContent("CodeWriter.Append(string?)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(string? value)
                                {
                                    ThrowIfDisposed();
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        WriteIndentIfNeeded();
                                        builder.Append(value);
                                    }

                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Append_ReadOnlySpan_Content()
    {
        string content = GetMethodContent("CodeWriter.Append(System.ReadOnlySpan<char>)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(global::System.ReadOnlySpan<char> value)
                                {
                                    ThrowIfDisposed();
                                    if (value.Length > 0)
                                    {
                                        WriteIndentIfNeeded();
                                        // Consider allowing unsafe code in your project to use pointers here instead.
                                        builder.EnsureCapacity(builder.Length + value.Length);
                                        for (int i = 0; i < value.Length; i++)
                                        {
                                            builder.Append(value[i]);
                                        }
                                    }

                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Append_Char_Int_Content()
    {
        string content = GetMethodContent("CodeWriter.Append(char, int)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(char value, int repeatCount)
                                {
                                    ThrowIfDisposed();
                                    WriteIndentIfNeeded();
                                    builder.Append(value, repeatCount);
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Append_CharArray_StartIndex_CharCount_Content()
    {
        string content = GetMethodContent("CodeWriter.Append(char[], int, int)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(char[] value, int startIndex, int charCount)
                                {
                                    ThrowIfDisposed();
                                    WriteIndentIfNeeded();
                                    builder.Append(value, startIndex, charCount);
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_Content_NoIndent()
    {
        string content = GetMethodContent("CodeWriter.AppendLine()");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine()
                                {
                                    ThrowIfDisposed();
                                    builder.Append('\n');
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_Content_WithIndent()
    {
        string content = GetMethodContent("CodeWriter.AppendLine()", "CodeWriter.WriteIndentIfNeeded()");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine()
                                {
                                    ThrowIfDisposed();
                                    builder.Append('\n');
                                    shouldWriteIndent = true;
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_String_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(string?)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(string? value)
                                {
                                    ThrowIfDisposed();
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        WriteIndentIfNeeded();
                                        builder.Append(value);
                                        builder.Append('\n');
                                        shouldWriteIndent = true;
                                    }

                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_ReadOnlySpan_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(System.ReadOnlySpan<char>)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(global::System.ReadOnlySpan<char> value)
                                {
                                    ThrowIfDisposed();
                                    if (value.Length > 0)
                                    {
                                        WriteIndentIfNeeded();
                                        // Consider allowing unsafe code in your project to use pointers here instead.
                                        builder.EnsureCapacity(builder.Length + value.Length);
                                        for (int i = 0; i < value.Length; i++)
                                        {
                                            builder.Append(value[i]);
                                        }
                                        builder.Append('\n');
                                        shouldWriteIndent = true;
                                    }

                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_Char_Int_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(char, int)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(char value, int repeatCount)
                                {
                                    ThrowIfDisposed();
                                    WriteIndentIfNeeded();
                                    builder.Append(value, repeatCount);
                                    builder.Append('\n');
                                    shouldWriteIndent = true;
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_CharArray_StartIndex_CharCount_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(char[], int, int)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(char[] value, int startIndex, int charCount)
                                {
                                    ThrowIfDisposed();
                                    WriteIndentIfNeeded();
                                    builder.Append(value, startIndex, charCount);
                                    builder.Append('\n');
                                    shouldWriteIndent = true;
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_Char_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(char)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(char value)
                                {
                                    ThrowIfDisposed();
                                    WriteIndentIfNeeded();
                                    builder.Append(value);
                                    builder.Append('\n');
                                    shouldWriteIndent = true;
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_Int_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(int)");
        const string expected = $$"""
                                  [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                  public partial global::Hertzole.SourceGen.CodeWriter AppendLine(int value)
                                  {
                                      {{NEW_LINE_FORMATTABLE}}
                                  }
                                  """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_Object_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(object)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(object value)
                                {
                                    return value == null ? this : AppendLine(value.ToString());
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_Byte_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(byte)");
        string expected = $$"""
                            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                            public partial global::Hertzole.SourceGen.CodeWriter AppendLine(byte value)
                            {
                                {{NEW_LINE_FORMATTABLE}}
                            }
                            """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_Sbyte_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(sbyte)");
        const string expected = $$"""
                                  [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                  public partial global::Hertzole.SourceGen.CodeWriter AppendLine(sbyte value)
                                  {
                                      {{NEW_LINE_FORMATTABLE}}
                                  }
                                  """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_Short_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(short)");
        const string expected = $$"""
                                  [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                  public partial global::Hertzole.SourceGen.CodeWriter AppendLine(short value)
                                  {
                                      {{NEW_LINE_FORMATTABLE}}
                                  }
                                  """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_Ushort_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(ushort)");
        const string expected = $$"""
                                  [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                  public partial global::Hertzole.SourceGen.CodeWriter AppendLine(ushort value)
                                  {
                                      {{NEW_LINE_FORMATTABLE}}
                                  }
                                  """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_Uint_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(uint)");
        const string expected = $$"""
                                  [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                  public partial global::Hertzole.SourceGen.CodeWriter AppendLine(uint value)
                                  {
                                      {{NEW_LINE_FORMATTABLE}}
                                  }
                                  """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_Long_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(long)");
        const string expected = $$"""
                                  [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                  public partial global::Hertzole.SourceGen.CodeWriter AppendLine(long value)
                                  {
                                      {{NEW_LINE_FORMATTABLE}}
                                  }
                                  """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_Ulong_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(ulong)");
        const string expected = $$"""
                                  [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                  public partial global::Hertzole.SourceGen.CodeWriter AppendLine(ulong value)
                                  {
                                      {{NEW_LINE_FORMATTABLE}}
                                  }
                                  """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_Float_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(float)");
        const string expected = $$"""
                                  [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                  public partial global::Hertzole.SourceGen.CodeWriter AppendLine(float value)
                                  {
                                      {{NEW_LINE_FORMATTABLE}}
                                  }
                                  """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_Double_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(double)");
        const string expected = $$"""
                                  [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                  public partial global::Hertzole.SourceGen.CodeWriter AppendLine(double value)
                                  {
                                      {{NEW_LINE_FORMATTABLE}}
                                  }
                                  """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_Decimal_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(decimal)");
        const string expected = $$"""
                                  [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                  public partial global::Hertzole.SourceGen.CodeWriter AppendLine(decimal value)
                                  {
                                      {{NEW_LINE_FORMATTABLE}}
                                  }
                                  """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendLine_Bool_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendLine(bool)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(bool value)
                                {
                                    return AppendLine(value ? "true" : "false");
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendNamespace_INamespaceSymbol_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendNamespace(Microsoft.CodeAnalysis.INamespaceSymbol?)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendNamespace(global::Microsoft.CodeAnalysis.INamespaceSymbol? symbol)
                                {
                                    ThrowIfDisposed();
                                    if (symbol == null || symbol.IsGlobalNamespace)
                                    {
                                        return this;
                                    }

                                    if (hasNamespace)
                                    {
                                        return this;
                                    }

                                    return AppendNamespace(symbol.ToDisplayString());
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendNamespace_String_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendNamespace(string)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendNamespace(string value)
                                {
                                    ThrowIfDisposed();
                                    if (string.IsNullOrEmpty(value))
                                    {
                                        return this;
                                    }

                                    hasNamespace = true;
                                    builder.Append("namespace ");
                                    builder.AppendLine(value);
                                    builder.AppendLine("{");
                                    Indent++;
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendNamespace_String_WithToString_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendNamespace(string)", "CodeWriter.ToString()");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendNamespace(string value)
                                {
                                    ThrowIfDisposed();
                                    if (string.IsNullOrEmpty(value))
                                    {
                                        return this;
                                    }

                                    hasNamespace = true;
                                    builder.Append("namespace ");
                                    builder.AppendLine(value);
                                    builder.AppendLine("{");
                                    Indent++;
                                    hasWrittenNamespace = false;
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendNamespace_String_WithWriteIndentIfNeeded_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendNamespace(string)", "CodeWriter.WriteIndentIfNeeded()");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendNamespace(string value)
                                {
                                    ThrowIfDisposed();
                                    if (string.IsNullOrEmpty(value))
                                    {
                                        return this;
                                    }

                                    hasNamespace = true;
                                    builder.Append("namespace ");
                                    builder.AppendLine(value);
                                    builder.AppendLine("{");
                                    Indent++;
                                    shouldWriteIndent = true;
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendNamespace_String_WithToStringAndWriteIndentIfNeeded_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendNamespace(string)", "CodeWriter.ToString()", "CodeWriter.WriteIndentIfNeeded()");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendNamespace(string value)
                                {
                                    ThrowIfDisposed();
                                    if (string.IsNullOrEmpty(value))
                                    {
                                        return this;
                                    }

                                    hasNamespace = true;
                                    builder.Append("namespace ");
                                    builder.AppendLine(value);
                                    builder.AppendLine("{");
                                    Indent++;
                                    hasWrittenNamespace = false;
                                    shouldWriteIndent = true;
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendGeneratedCodeAttribute_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendGeneratedCodeAttribute(string, string)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendGeneratedCodeAttribute(string generator, string version)
                                {
                                    ThrowIfDisposed();
                                    WriteIndentIfNeeded();
                                    builder.Append("[global::System.CodeDom.Compiler.GeneratedCode(\"");
                                    builder.Append(generator);
                                    builder.Append("\", \"");
                                    builder.Append(version);
                                    builder.Append("\")]\n");
                                    shouldWriteIndent = true;
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendExcludeFromCodeCoverageAttribute_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendExcludeFromCodeCoverageAttribute()");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendExcludeFromCodeCoverageAttribute()
                                {
                                    return AppendLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendConditionalSymbol_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendConditionalSymbol(string?)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendConditionalSymbol(string? condition)
                                {
                                    ThrowIfDisposed();
                                    if (string.IsNullOrWhiteSpace(condition))
                                    {
                                        return this;
                                    }

                                    global::System.ReadOnlySpan<char> span = global::System.MemoryExtensions.Trim(global::System.MemoryExtensions.AsSpan(condition));
                                    int indent = Indent;
                                    Indent = 0;
                                    if (global::System.MemoryExtensions.StartsWith(span, "if "))
                                    {
                                        builder.Append('#');
                                    }
                                    else if (!global::System.MemoryExtensions.StartsWith(span, "#if "))
                                    {
                                        builder.Append("#if ");
                                    }

                                    Append(span);
                                    builder.Append('\n');
                                    Indent = indent;
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendPreprocessorSymbol_Content()
    {
        string content = GetMethodContent("CodeWriter.AppendPreprocessorSymbol(string?)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendPreprocessorSymbol(string? value)
                                {
                                    ThrowIfDisposed();
                                    if (string.IsNullOrWhiteSpace(value))
                                    {
                                        return this;
                                    }

                                    int indent = Indent;
                                    Indent = 0;
                                    if (value![0] != '#')
                                    {
                                        builder.Append('#');
                                    }

                                    builder.Append(value);
                                    builder.Append('\n');
                                    Indent = indent;
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Clear_Content_NoWrittenAnything()
    {
        string content = GetMethodContent("CodeWriter.Clear()");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Clear()
                                {
                                    ThrowIfDisposed();
                                    Indent = 0;
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Clear_Content_WithAppend()
    {
        string content = GetMethodContent("CodeWriter.Clear()", "CodeWriter.Append(string)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Clear()
                                {
                                    ThrowIfDisposed();
                                    Indent = 0;
                                    builder.Clear();
                                    shouldWriteIndent = false;
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Clear_Content_WithAppendNamespaceAndToString()
    {
        string content = GetMethodContent("CodeWriter.Clear()", "CodeWriter.AppendNamespace(string)", "CodeWriter.ToString()");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Clear()
                                {
                                    ThrowIfDisposed();
                                    Indent = 0;
                                    builder.Clear();
                                    shouldWriteIndent = false;
                                    hasWrittenNamespace = false;
                                    hasNamespace = false;
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Clear_Content_WithAppendNullableAndToString()
    {
        string content = GetMethodContent("CodeWriter.Clear()", "CodeWriter.AppendNullable()", "CodeWriter.ToString()");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Clear()
                                {
                                    ThrowIfDisposed();
                                    Indent = 0;
                                    builder.Clear();
                                    shouldWriteIndent = false;
                                    isNullable = false;
                                    return this;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void ToString_Content_NoWrittenAnything()
    {
        string content = GetMethodContent("CodeWriter.ToString()");
        const string expected = """
                                public override partial string ToString()
                                {
                                    ThrowIfDisposed();
                                    return string.Empty;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void ToString_Content_WithAppend()
    {
        string content = GetMethodContent("CodeWriter.ToString()", "CodeWriter.Append(string)");
        const string expected = """
                                public override partial string ToString()
                                {
                                    ThrowIfDisposed();
                                    if (builder.Length == 0)
                                    {
                                        return string.Empty;
                                    }

                                    // Trim the last newline, if present.
                                    if (builder[builder.Length - 1] == '\n')
                                    {
                                        builder.Remove(builder.Length - 1, 1);
                                    }

                                    return builder.ToString();
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void ToString_Content_WithAppendNamespace()
    {
        string content = GetMethodContent("CodeWriter.ToString()", "CodeWriter.AppendNamespace(string)");
        const string expected = """
                                public override partial string ToString()
                                {
                                    ThrowIfDisposed();
                                    if (builder.Length == 0)
                                    {
                                        return string.Empty;
                                    }

                                    if (hasNamespace && !hasWrittenNamespace)
                                    {
                                        if (builder[builder.Length - 1] != '\n')
                                        {
                                            builder.Append('\n');
                                        }

                                        Indent--;
                                        builder.Append("}\n");
                                        hasWrittenNamespace = true;
                                        hasNamespace = false;
                                    }

                                    // Trim the last newline, if present.
                                    if (builder[builder.Length - 1] == '\n')
                                    {
                                        builder.Remove(builder.Length - 1, 1);
                                    }

                                    return builder.ToString();
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void ToString_Content_WithAppendNullable()
    {
        string content = GetMethodContent("CodeWriter.ToString()", "CodeWriter.AppendNullable()");
        const string expected = """
                                public override partial string ToString()
                                {
                                    ThrowIfDisposed();
                                    if (builder.Length == 0)
                                    {
                                        return string.Empty;
                                    }

                                    if (isNullable)
                                    {
                                        builder.Append("#nullable restore\n");
                                    }

                                    // Trim the last newline, if present.
                                    if (builder[builder.Length - 1] == '\n')
                                    {
                                        builder.Remove(builder.Length - 1, 1);
                                    }

                                    return builder.ToString();
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void ToString_Content_WithAppendNamespaceAndAppendNullable()
    {
        string content = GetMethodContent("CodeWriter.ToString()", "CodeWriter.AppendNamespace(string)", "CodeWriter.AppendNullable()");
        const string expected = """
                                public override partial string ToString()
                                {
                                    ThrowIfDisposed();
                                    if (builder.Length == 0)
                                    {
                                        return string.Empty;
                                    }

                                    if (hasNamespace && !hasWrittenNamespace)
                                    {
                                        if (builder[builder.Length - 1] != '\n')
                                        {
                                            builder.Append('\n');
                                        }

                                        Indent--;
                                        builder.Append("}\n");
                                        hasWrittenNamespace = true;
                                        hasNamespace = false;
                                    }

                                    if (isNullable)
                                    {
                                        builder.Append("#nullable restore\n");
                                    }

                                    // Trim the last newline, if present.
                                    if (builder[builder.Length - 1] == '\n')
                                    {
                                        builder.Remove(builder.Length - 1, 1);
                                    }

                                    return builder.ToString();
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void WriteIndentIfNeeded_Content()
    {
        string content = GetMethodContent("CodeWriter.WriteIndentIfNeeded()");
        const string expected = """
                                private void WriteIndentIfNeeded()
                                {
                                    if (!shouldWriteIndent)
                                    {
                                        return;
                                    }

                                    shouldWriteIndent = false;
                                    builder.Append(' ', Indent * 4);
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void WithBlock_Content()
    {
        string content = GetMethodContent("CodeWriter.WithBlock()");
        const string expected = """
                                public partial global::Hertzole.SourceGen.CodeWriter.BlockScope WithBlock()
                                {
                                    ThrowIfDisposed();
                                    return new global::Hertzole.SourceGen.CodeWriter.BlockScope(this);
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void WithIndent_Content()
    {
        string content = GetMethodContent("CodeWriter.WithIndent(int)");
        const string expected = """
                                public partial global::Hertzole.SourceGen.CodeWriter.IndentScope WithIndent(int newIndent)
                                {
                                    ThrowIfDisposed();
                                    return new global::Hertzole.SourceGen.CodeWriter.IndentScope(this, newIndent);
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AppendNullable_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendNullable()
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendNullable()", expected);
    }

    [Test]
    public void Append_String_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(string? value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(string?)", expected);
    }

    [Test]
    public void Append_ReadOnlySpan_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(global::System.ReadOnlySpan<char> value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(System.ReadOnlySpan<char>)", expected);
    }

    [Test]
    public void Append_Char_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(char value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(char)", expected);
    }

    [Test]
    public void Append_Char_Int_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(char value, int repeatCount)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(char, int)", expected);
    }

    [Test]
    public void Append_CharArray_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(char[] value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(char[])", expected);
    }

    [Test]
    public void Append_CharArray_StartIndex_CharCount_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(char[] value, int startIndex, int charCount)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(char[], int, int)", expected);
    }

    [Test]
    public void Append_Byte_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(byte value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(byte)", expected);
    }

    [Test]
    public void Append_Sbyte_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(sbyte value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(sbyte)", expected);
    }

    [Test]
    public void Append_Short_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(short value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(short)", expected);
    }

    [Test]
    public void Append_Ushort_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(ushort value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(ushort)", expected);
    }

    [Test]
    public void Append_Int_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(int value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(int)", expected);
    }

    [Test]
    public void Append_Uint_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(uint value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(uint)", expected);
    }

    [Test]
    public void Append_Long_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(long value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(long)", expected);
    }

    [Test]
    public void Append_Ulong_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(ulong value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(ulong)", expected);
    }

    [Test]
    public void Append_Float_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(float value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(float)", expected);
    }

    [Test]
    public void Append_Double_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(double value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(double)", expected);
    }

    [Test]
    public void Append_Decimal_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(decimal value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(decimal)", expected);
    }

    [Test]
    public void Append_Bool_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(bool value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(bool)", expected);
    }

    [Test]
    public void Append_Object_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Append(object value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Append(object)", expected);
    }

    [Test]
    public void AppendLine_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine()
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine()", expected);
    }

    [Test]
    public void AppendLine_String_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(string? value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(string?)", expected);
    }

    [Test]
    public void AppendLine_ReadOnlySpan_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(global::System.ReadOnlySpan<char> value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(System.ReadOnlySpan<char>)", expected);
    }

    [Test]
    public void AppendLine_Char_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(char value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(char)", expected);
    }

    [Test]
    public void AppendLine_Char_Int_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(char value, int repeatCount)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(char, int)", expected);
    }

    [Test]
    public void AppendLine_CharArray_StartIndex_CharCount_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(char[] value, int startIndex, int charCount)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(char[], int, int)", expected);
    }

    [Test]
    public void AppendLine_Byte_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(byte value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(byte)", expected);
    }

    [Test]
    public void AppendLine_Sbyte_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(sbyte value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(sbyte)", expected);
    }

    [Test]
    public void AppendLine_Short_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(short value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(short)", expected);
    }

    [Test]
    public void AppendLine_Ushort_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(ushort value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(ushort)", expected);
    }

    [Test]
    public void AppendLine_Int_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(int value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(int)", expected);
    }

    [Test]
    public void AppendLine_Uint_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(uint value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(uint)", expected);
    }

    [Test]
    public void AppendLine_Long_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(long value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(long)", expected);
    }

    [Test]
    public void AppendLine_Ulong_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(ulong value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(ulong)", expected);
    }

    [Test]
    public void AppendLine_Float_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(float value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(float)", expected);
    }

    [Test]
    public void AppendLine_Double_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(double value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(double)", expected);
    }

    [Test]
    public void AppendLine_Decimal_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(decimal value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(decimal)", expected);
    }

    [Test]
    public void AppendLine_Bool_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(bool value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(bool)", expected);
    }

    [Test]
    public void AppendLine_Object_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendLine(object value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendLine(object)", expected);
    }

    [Test]
    public void AppendNamespace_String_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter AppendNamespace(string value)
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.AppendNamespace(string)", expected);
    }

    [Test]
    public void Clear_Content_NotCalled()
    {
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::Hertzole.SourceGen.CodeWriter Clear()
                                {
                                    return this;
                                }
                                """;

        EmptyContentTest("CodeWriter.Clear()", expected);
    }

    [Test]
    public void ToString_Content_NotCalled()
    {
        const string expected = """
                                public override partial string ToString()
                                {
                                    return string.Empty;
                                }
                                """;

        EmptyContentTest("CodeWriter.ToString()", expected);
    }

    [Test]
    public void WithBlock_Content_NotCalled()
    {
        const string expected = """
                                public partial global::Hertzole.SourceGen.CodeWriter.BlockScope WithBlock()
                                {
                                    return default;
                                }
                                """;

        EmptyContentTest("CodeWriter.WithBlock()", expected);
    }

    [Test]
    public void WithIndent_Content_NotCalled()
    {
        const string expected = """
                                public partial global::Hertzole.SourceGen.CodeWriter.IndentScope WithIndent(int newIndent)
                                {
                                    return default;
                                }
                                """;

        EmptyContentTest("CodeWriter.WithIndent(int)", expected);
    }

    [Test]
    public void Field_Builder_Content()
    {
        string content = GetFieldContent("CodeWriter.builder", "CodeWriter.Append(string)");
        const string expected = """
                                private global::System.Text.StringBuilder builder;
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Field_ShouldWriteIndent_Content()
    {
        string content = GetFieldContent("CodeWriter.shouldWriteIndent", "CodeWriter.WriteIndentIfNeeded()");
        const string expected = """
                                private bool shouldWriteIndent = false;
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Field_HasNamespace_Content()
    {
        string content = GetFieldContent("CodeWriter.hasNamespace", "CodeWriter.AppendNamespace(string)");
        const string expected = """
                                private bool hasNamespace = false;
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Field_HasWrittenNamespace_Content()
    {
        string content = GetFieldContent("CodeWriter.hasWrittenNamespace", "CodeWriter.AppendNamespace(string)", "CodeWriter.ToString()");
        const string expected = """
                                private bool hasWrittenNamespace = false;
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Field_IsNullable_Content()
    {
        string content = GetFieldContent("CodeWriter.isNullable", "CodeWriter.AppendNullable()", "CodeWriter.ToString()");
        const string expected = """
                                private bool isNullable = false;
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Field_IsDisposed_Content()
    {
        string content = GetFieldContent("CodeWriter.isDisposed", "CodeWriter.Dispose()");
        const string expected = """
                                private bool isDisposed = false;
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Call_WithBlock()
    {
        string[] expectedMethods =
        [
            "CodeWriter.WithBlock()",
            "CodeWriter.BlockScope.BlockScope(Hertzole.SourceGen.CodeWriter)",
            "CodeWriter.BlockScope.Dispose()",
            "CodeWriter.WriteIndentIfNeeded()",
            "CodeWriter.ThrowIfDisposed()",
            "CodeWriter.CodeWriter()",
            "CodeWriter.Dispose()",
            "CodeWriter.AppendLine(char)"
        ];

        AssertCallingMethodCreatesMethods(writer => { writer.AppendLine("new CodeWriter().WithBlock();"); }, expectedMethods);
    }

    [Test]
    public void Call_WithIndent()
    {
        string[] expectedMethods =
        [
            "CodeWriter.WithIndent(int)",
            "CodeWriter.IndentScope.IndentScope(Hertzole.SourceGen.CodeWriter, int)",
            "CodeWriter.IndentScope.Dispose()",
            "CodeWriter.ThrowIfDisposed()",
            "CodeWriter.CodeWriter()"
        ];

        AssertCallingMethodCreatesMethods(writer => { writer.AppendLine("new CodeWriter().WithIndent(0);"); }, expectedMethods);
    }

    [Test]
    public void Property_Indent_Content()
    {
        string content = GetTypeContent();
        Assert.That(content, Does.Contain("public int Indent { get; set; }"));
    }

    [Test]
    public void NestedType_BlockScope_Exists()
    {
        Assert.That(Generator.TypesToGenerate["CodeWriter"].Types, Does.ContainKey("BlockScope"));
    }

    [Test]
    public void NestedType_IndentScope_Exists()
    {
        Assert.That(Generator.TypesToGenerate["CodeWriter"].Types, Does.ContainKey("IndentScope"));
    }

    [Test]
    public void NestedType_BlockScope_Signature()
    {
        TypeSource blockScope = Generator.TypesToGenerate["CodeWriter"].Types!["BlockScope"];
        Assert.That(blockScope.Signature, Does.Contain("BlockScope"));
        Assert.That(blockScope.Signature, Does.Contain("IDisposable"));
    }

    [Test]
    public void NestedType_IndentScope_Signature()
    {
        TypeSource indentScope = Generator.TypesToGenerate["CodeWriter"].Types!["IndentScope"];
        Assert.That(indentScope.Signature, Does.Contain("IndentScope"));
        Assert.That(indentScope.Signature, Does.Contain("IDisposable"));
    }

    [Test]
    public void NestedType_BlockScope_HasMethod_BlockScope()
    {
        MethodSource[] methods = Generator.TypesToGenerate["CodeWriter"].Types!["BlockScope"].Methods!;
        Assert.That(methods, Has.One.Matches<MethodSource>(m => m.Name == "BlockScope"));
    }

    [Test]
    public void NestedType_BlockScope_HasMethod_Dispose()
    {
        MethodSource[] methods = Generator.TypesToGenerate["CodeWriter"].Types!["BlockScope"].Methods!;
        Assert.That(methods, Has.One.Matches<MethodSource>(m => m.Name == "Dispose"));
    }

    [Test]
    public void NestedType_IndentScope_HasMethod_IndentScope()
    {
        MethodSource[] methods = Generator.TypesToGenerate["CodeWriter"].Types!["IndentScope"].Methods!;
        Assert.That(methods, Has.One.Matches<MethodSource>(m => m.Name == "IndentScope"));
    }

    [Test]
    public void NestedType_IndentScope_HasMethod_Dispose()
    {
        MethodSource[] methods = Generator.TypesToGenerate["CodeWriter"].Types!["IndentScope"].Methods!;
        Assert.That(methods, Has.One.Matches<MethodSource>(m => m.Name == "Dispose"));
    }

    [Test]
    public void NestedType_BlockScope_HasField_Writer()
    {
        Assert.That(Generator.TypesToGenerate["CodeWriter"].Types!["BlockScope"].Fields, Does.ContainKey("writer"));
    }

    [Test]
    public void NestedType_IndentScope_HasField_Writer()
    {
        Assert.That(Generator.TypesToGenerate["CodeWriter"].Types!["IndentScope"].Fields, Does.ContainKey("writer"));
    }

    [Test]
    public void NestedType_IndentScope_HasField_OriginalIndent()
    {
        Assert.That(Generator.TypesToGenerate["CodeWriter"].Types!["IndentScope"].Fields, Does.ContainKey("originalIndent"));
    }

    [Test]
    public void BlockScope_Constructor_Content()
    {
        string content = GetMethodContent("CodeWriter.BlockScope.BlockScope(Hertzole.SourceGen.CodeWriter)");
        const string expected = """
                                public partial BlockScope(global::Hertzole.SourceGen.CodeWriter writer)
                                {
                                    this.writer = writer;
                                    writer.AppendLine('{');
                                    writer.Indent++;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void BlockScope_Dispose_Content()
    {
        string content = GetMethodContent("CodeWriter.BlockScope.Dispose()");
        const string expected = """
                                public partial void Dispose()
                                {
                                    writer.Indent--;
                                    writer.AppendLine('}');
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void IndentScope_Constructor_Content()
    {
        string content = GetMethodContent("CodeWriter.IndentScope.IndentScope(Hertzole.SourceGen.CodeWriter, int)");
        const string expected = """
                                public partial IndentScope(global::Hertzole.SourceGen.CodeWriter writer, int newIndent)
                                {
                                    this.writer = writer;
                                    originalIndent = writer.Indent;
                                    writer.Indent = newIndent;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void IndentScope_Dispose_Content()
    {
        string content = GetMethodContent("CodeWriter.IndentScope.Dispose()");
        const string expected = """
                                public partial void Dispose()
                                {
                                    writer.Indent = originalIndent;
                                }
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void BlockScope_Constructor_Content_NotCalled()
    {
        const string expected = """
                                public partial BlockScope(global::Hertzole.SourceGen.CodeWriter writer)
                                {
                                }
                                """;

        EmptyContentTest("CodeWriter.BlockScope.BlockScope(Hertzole.SourceGen.CodeWriter)", expected);
    }

    [Test]
    public void BlockScope_Dispose_Content_NotCalled()
    {
        const string expected = """
                                public partial void Dispose()
                                {
                                }
                                """;

        EmptyContentTest("CodeWriter.BlockScope.Dispose()", expected);
    }

    [Test]
    public void IndentScope_Constructor_Content_NotCalled()
    {
        const string expected = """
                                public partial IndentScope(global::Hertzole.SourceGen.CodeWriter writer, int newIndent)
                                {
                                }
                                """;

        EmptyContentTest("CodeWriter.IndentScope.IndentScope(Hertzole.SourceGen.CodeWriter, int)", expected);
    }

    [Test]
    public void IndentScope_Dispose_Content_NotCalled()
    {
        const string expected = """
                                public partial void Dispose()
                                {
                                }
                                """;

        EmptyContentTest("CodeWriter.IndentScope.Dispose()", expected);
    }

    [Test]
    public void Field_BlockScope_Writer_Content()
    {
        string content = GetFieldContent("CodeWriter.BlockScope.writer", "CodeWriter.BlockScope.BlockScope(Hertzole.SourceGen.CodeWriter)");
        const string expected = """
                                private readonly global::Hertzole.SourceGen.CodeWriter writer;
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Field_IndentScope_Writer_Content()
    {
        string content = GetFieldContent("CodeWriter.IndentScope.writer", "CodeWriter.IndentScope.IndentScope(Hertzole.SourceGen.CodeWriter, int)");
        const string expected = """
                                private readonly global::Hertzole.SourceGen.CodeWriter writer;
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Field_IndentScope_OriginalIndent_Content()
    {
        string content = GetFieldContent("CodeWriter.IndentScope.originalIndent", "CodeWriter.IndentScope.IndentScope(Hertzole.SourceGen.CodeWriter, int)");
        const string expected = """
                                private readonly int originalIndent;
                                """;

        Assert.That(content, Is.EqualTo(expected));
    }
}