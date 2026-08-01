using System.Collections.Generic;

namespace Hertzole.SourceGenUtils.Helpers;

internal partial class CodeWriterGenerator
{
    private static MethodSource[] GetAppendLineMethods()
    {
        return
        [
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine()",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in context) =>
                {
                    writer.AppendLine(disposed_call);
                    writer.AppendLine(new_line);
                    if (context.HasCalledMethod(CODE_WRITER + ".WriteIndentIfNeeded()"))
                    {
                        writer.AppendLine("shouldWriteIndent = true;");
                    }

                    writer.AppendLine(return_this);
                },
                Dependencies = [dispose, throw_if_disposed],
                EmptyStub = return_this,
                Trivia = new TriviaSource
                {
                    Summary = "Appends a newline to the current line.",
                    Returns = APPEND_RETURN_TRIVIA
                }
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(string? value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) =>
                {
                    writer.AppendLine(disposed_call);
                    writer.AppendLine("if (!string.IsNullOrEmpty(value))");
                    using (writer.WithBlock(true))
                    {
                        writer.AppendLine("WriteIndentIfNeeded();");
                        writer.AppendLine("builder.Append(value);");
                        writer.AppendLine("builder.Append('\\n');");
                        writer.AppendLine("shouldWriteIndent = true;");
                    }

                    writer.AppendLine(return_this);
                },
                Dependencies = appendDependencies,
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia("string")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine({GLOBAL_R_SPAN}<char> value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) =>
                {
                    writer.AppendLine("Append(value);");
                    writer.AppendLine(new_line);
                    writer.AppendLine("shouldWriteIndent = true;");
                    writer.AppendLine(return_this);
                },
                Dependencies = [.. appendDependencies, $"{CODE_WRITER}.Append({R_SPAN}<char>)"],
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia($"{GLOBAL_R_SPAN}{{Char}}", "ReadOnlySpan<char>")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine({GLOBAL_R_MEMORY}<char> value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => { writer.AppendLine("return AppendLine(value.Span);"); },
                Dependencies = [CODE_WRITER + $".AppendLine({R_SPAN}<char>)"],
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia($"{GLOBAL_R_MEMORY}{{Char}}", "ReadOnlyMemory<char>")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine({GLOBAL_ARRAY_BUILDER}<char> value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => { writer.AppendLine("return AppendLine(value.AsSpan());"); },
                Dependencies = [CODE_WRITER + $".AppendLine({R_SPAN}<char>)", $"{ARRAY_BUILDER}.AsSpan()"],
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia($"{GLOBAL_ARRAY_BUILDER}{{char}}", "ArrayBuilder<char>")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(char value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = AppendLineImplementation,
                Dependencies = appendDependencies,
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia("char")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(char value, int repeatCount)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) =>
                {
                    writer.AppendLine(disposed_call);
                    writer.AppendLine("WriteIndentIfNeeded();");
                    writer.AppendLine("builder.Append(value, repeatCount);");
                    writer.AppendLine(new_line);
                    writer.AppendLine("shouldWriteIndent = true;");
                    writer.AppendLine("return this;");
                },
                Dependencies = appendDependencies,
                EmptyStub = return_this,
                Trivia = new TriviaSource
                {
                    Summary = "Appends the specified number of copies of the specified <see cref=\"char\"/> followed by a newline to the current line.",
                    Parameters = new Dictionary<string, string>
                    {
                        ["value"] = "The char to append.",
                        ["repeatCount"] = "How many times the char should be inserted."
                    },
                    Returns = APPEND_RETURN_TRIVIA
                }
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(char[] value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = AppendLineImplementation,
                Dependencies = appendDependencies,
                EmptyStub = return_this,
                Trivia = new TriviaSource
                {
                    Summary = "Appends the specified <see cref=\"char\"/> array followed by a newline to the current line.",
                    Parameters = new Dictionary<string, string>
                    {
                        ["value"] = "The char array to append."
                    },
                    Returns = APPEND_RETURN_TRIVIA
                }
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(char[] value, int startIndex, int charCount)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) =>
                {
                    writer.AppendLine(disposed_call);
                    writer.AppendLine("WriteIndentIfNeeded();");
                    writer.AppendLine("builder.Append(value, startIndex, charCount);");
                    writer.AppendLine(new_line);
                    writer.AppendLine("shouldWriteIndent = true;");
                    writer.AppendLine("return this;");
                },
                Dependencies = appendDependencies,
                EmptyStub = return_this,
                Trivia = new TriviaSource
                {
                    Summary = "Appends a subarray of characters followed by a newline to the current line.",
                    Parameters = new Dictionary<string, string>
                    {
                        ["value"] = "The character array to append.",
                        ["startIndex"] = "The starting position in the character array.",
                        ["charCount"] = "The number of characters to append."
                    },
                    Returns = APPEND_RETURN_TRIVIA
                }
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(byte value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, false),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia("byte")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(byte value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, true),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineFormatTrivia("byte")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(sbyte value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, false),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia("sbyte")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(sbyte value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, true),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineFormatTrivia("sbyte")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(short value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, false),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia("short")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(short value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, true),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineFormatTrivia("short")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(ushort value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, false),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia("ushort")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(ushort value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, true),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineFormatTrivia("ushort")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(int value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, false),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia("int")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(int value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, true),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineFormatTrivia("int")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(uint value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, false),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia("uint")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(uint value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, true),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineFormatTrivia("uint")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(long value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, false),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia("long")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(long value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, true),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineFormatTrivia("long")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(ulong value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, false),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia("ulong")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(ulong value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, true),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineFormatTrivia("ulong")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(float value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, false),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia("float")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(float value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, true),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineFormatTrivia("float")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(double value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, false),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia("double")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(double value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, true),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineFormatTrivia("double")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(decimal value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, false),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia("decimal")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(decimal value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendLineFormattable(writer, true),
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineFormatTrivia("decimal")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(bool value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => { writer.AppendLine("return AppendLine(value ? \"true\" : \"false\");"); },
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia("bool")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(object value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => { writer.AppendLine("return value == null ? this : AppendLine(value.ToString());"); },
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = new TriviaSource
                {
                    Summary = "Appends the string representation of the specified object followed by a newline to the current line.",
                    Parameters = new Dictionary<string, string>
                    {
                        ["value"] = "The object to append."
                    },
                    Returns = APPEND_RETURN_TRIVIA
                }
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature =
                    $"public partial {GLOBAL_CODE_WRITER} AppendLine({GLOBAL_MS_CODE}.INamedTypeSymbol value, bool isPartial = true, bool appendNamespace = true)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) =>
                {
                    writer.AppendLine("Append(value, isPartial, appendNamespace);");
                    writer.AppendLine("AppendLine();");
                    writer.AppendLine(return_this);
                },
                Dependencies = [$"{CODE_WRITER}.Append({MS_CODE}.INamedTypeSymbol, bool, bool)", CODE_WRITER + ".AppendLine()"],
                EmptyStub = return_this,
                Trivia = new TriviaSource
                {
                    Summary =
                        $"Appends the string representation of the specified {GetTypeTriviaReference($"{GLOBAL_MS_CODE}.ITypeSymbol", "ITypeSymbol", out _)}, " +
                        $"along with its required declarations, followed by a newline.<br/>\n" +
                        $"Optionally inserts the <c>partial</c> keyword in the declaration if <paramref name=\"isPartial\"/> is <see langword=\"true\"/>.<br/>\n" +
                        $"Optionally inserts the namespace if <paramref name=\"appendNamespace\"/> is <see langword=\"true\"/>.",
                    Parameters = new Dictionary<string, string>
                    {
                        ["value"] = "The type to append.",
                        ["isPartial"] = "Whether to append the <c>partial</c> keyword in the declaration.",
                        ["appendNamespace"] = "Whether to append the namespace."
                    },
                    Returns = APPEND_RETURN_TRIVIA
                }
            }
        ];
    }

    private static void AppendLineFormattable(CodeWriter writer, bool isImplementation)
    {
        if (isImplementation)
        {
            writer.AppendLine("return AppendLine(value.ToString(format, provider));");
        }
        else
        {
            writer.AppendLine("return AppendLine(value.ToString(\"G\", global::System.Globalization.CultureInfo.InvariantCulture));");
        }
    }

    private static void AppendLineImplementation(CodeWriter writer, in ImplementationContext ctx)
    {
        writer.AppendLine(disposed_call);
        writer.AppendLine("WriteIndentIfNeeded();");
        writer.AppendLine("builder.Append(value);");
        writer.AppendLine(new_line);
        writer.AppendLine("shouldWriteIndent = true;");
        writer.AppendLine(return_this);
    }

    private static TriviaSource CreateAppendLineTrivia(string type, string? displayName = null)
    {
        string typeRef = GetTypeTriviaReference(type, displayName, out string name);

        return new TriviaSource
        {
            Summary = $"Appends {typeRef} followed by a newline to the current line.",
            Parameters = new Dictionary<string, string>
            {
                ["value"] = $"The {name} to insert."
            },
            Returns = APPEND_RETURN_TRIVIA
        };
    }

    private static TriviaSource CreateAppendLineFormatTrivia(string type, string? displayName = null)
    {
        string typeRef = GetTypeTriviaReference(type, displayName, out string name);

        return new TriviaSource
        {
            Summary = $"Appends {typeRef} with the specified format followed by a newline to the current line.",
            Parameters = new Dictionary<string, string>
            {
                ["value"] = $"The {name} to insert.",
                ["format"] = FORMAT_TRIVIA,
                ["provider"] = FORMAT_PROVIDER_TRIVIA
            },
            Returns = APPEND_RETURN_TRIVIA
        };
    }
}