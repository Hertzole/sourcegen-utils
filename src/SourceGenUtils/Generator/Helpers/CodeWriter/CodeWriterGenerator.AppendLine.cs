using System.Collections.Generic;
using System.Text;

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
                    Returns = "The current code writer instance."
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
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(global::System.ReadOnlySpan<char> value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in context) =>
                {
                    writer.AppendLine(disposed_call);

                    writer.AppendLine("if (value.Length > 0)");
                    using (writer.WithBlock(true))
                    {
                        writer.AppendLine("WriteIndentIfNeeded();");

                        if (context.AllowUnsafe)
                        {
                            writer.AppendLine("unsafe");
                            using (writer.WithBlock())
                            {
                                writer.AppendLine("fixed (char* buffer = value)");
                                using (writer.WithBlock())
                                {
                                    writer.AppendLine("builder.Append(buffer, value.Length);");
                                }
                            }
                        }
                        else
                        {
                            writer.AppendLine("// Consider allowing unsafe code in your project to use pointers here instead.");
                            writer.AppendLine("builder.EnsureCapacity(builder.Length + value.Length);");
                            writer.AppendLine("for (int i = 0; i < value.Length; i++)");
                            using (writer.WithBlock())
                            {
                                writer.AppendLine("builder.Append(value[i]);");
                            }
                        }

                        writer.AppendLine(new_line);
                        writer.AppendLine("shouldWriteIndent = true;");
                    }

                    writer.AppendLine(return_this);
                },
                Dependencies = appendDependencies,
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia("ReadOnlySpan&lt;char&gt;")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(global::System.ReadOnlyMemory<char> value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => { writer.AppendLine("return AppendLine(value.Span);"); },
                Dependencies = [CODE_WRITER + ".AppendLine(System.ReadOnlySpan<char>)"],
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia("ReadOnlyMemory<char>")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(global::{NAMESPACE}.ArrayBuilder<char> value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => { writer.AppendLine("return AppendLine(value.AsSpan());"); },
                Dependencies = [CODE_WRITER + ".AppendLine(System.ReadOnlySpan<char>)", ARRAY_BUILDER + ".AsSpan()"],
                EmptyStub = return_this,
                Trivia = CreateAppendLineTrivia("ArrayBuilder<char>")
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
                    Summary = "Appends the specified number of copies of the specified value followed by a newline to the current line.",
                    Parameters = new Dictionary<string, string>
                    {
                        ["value"] = "The value to append.",
                        ["repeatCount"] = "How many times the value should be inserted."
                    },
                    Returns = "The current code writer instance."
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
                    Returns = "The current code writer instance."
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
                Trivia = CreateAppendLineTrivia("byte")
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
                Trivia = CreateAppendLineTrivia("sbyte")
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
                Trivia = CreateAppendLineTrivia("short")
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
                Trivia = CreateAppendLineTrivia("ushort")
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
                Trivia = CreateAppendLineTrivia("int")
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
                Trivia = CreateAppendLineTrivia("uint")
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
                Trivia = CreateAppendLineTrivia("long")
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
                Trivia = CreateAppendLineTrivia("ulong")
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
                Trivia = CreateAppendLineTrivia("float")
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
                Trivia = CreateAppendLineTrivia("double")
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
                Trivia = CreateAppendLineTrivia("decimal")
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
                Trivia = CreateAppendLineTrivia("object")
            },
            new MethodSource
            {
                Name = "AppendLine",
                Signature =
                    $"public partial {GLOBAL_CODE_WRITER} AppendLine({GLOBAL_MS_CODE}.INamedTypeSymbol value, bool partial = true, bool appendNamespace = true)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) =>
                {
                    writer.AppendLine("Append(value, partial, appendNamespace);");
                    writer.AppendLine("AppendLine();");
                    writer.AppendLine(return_this);
                },
                Dependencies = [$"{CODE_WRITER}.Append({MS_CODE}.INamedTypeSymbol, bool, bool)", CODE_WRITER + ".AppendLine()"],
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia("object")
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

    private static TriviaSource CreateAppendLineTrivia(string type)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(type).Replace("<", "&lt;").Replace(">", "&gt;");

        return new TriviaSource
        {
            Summary = $"Appends <c>{sb}</c> followed by a newline to the current line.",
            Parameters = new Dictionary<string, string>
            {
                ["value"] = "The value to insert."
            },
            Returns = "The current code writer instance."
        };
    }
}