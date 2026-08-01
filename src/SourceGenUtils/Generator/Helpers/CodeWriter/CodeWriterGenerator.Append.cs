using System.Collections.Generic;

namespace Hertzole.SourceGenUtils.Helpers;

internal partial class CodeWriterGenerator
{
    private const string APPEND_RETURN_TRIVIA = "The current code writer instance after the append operation.";
    private const string FORMAT_TRIVIA = "A standard or custom numeric format string.";
    private const string FORMAT_PROVIDER_TRIVIA = "A optional object that supplies culture-specific formatting information.";

    private static MethodSource[] GetAppendMethods()
    {
        return
        [
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(string? value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) =>
                {
                    writer.AppendLine(disposed_call);
                    writer.AppendLine("if (!string.IsNullOrEmpty(value))");
                    using (writer.WithBlock(true))
                    {
                        writer.AppendLine("WriteIndentIfNeeded();");
                        writer.AppendLine("builder.Append(value);");
                    }

                    writer.AppendLine(return_this);
                },
                Dependencies = appendDependencies,
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia("string")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append({GLOBAL_R_SPAN}<char> value)",
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
                    }

                    writer.AppendLine(return_this);
                },
                Dependencies = appendDependencies,
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia($"{GLOBAL_R_SPAN}{{Char}}", "ReadOnlySpan<char>")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append({GLOBAL_R_MEMORY}<char> value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => { writer.AppendLine("return Append(value.Span);"); },
                Dependencies = [CODE_WRITER + $".Append({R_SPAN}<char>)"],
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia($"{GLOBAL_R_MEMORY}{{Char}}", "ReadOnlyMemory<char>")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append({GLOBAL_ARRAY_BUILDER}<char> value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => { writer.AppendLine("return Append(value.AsSpan());"); },
                Dependencies = [CODE_WRITER + $".Append({R_SPAN}<char>)", $"{ARRAY_BUILDER}.AsSpan()"],
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia($"{GLOBAL_ARRAY_BUILDER}{{Char}}", "ArrayBuilder<char>")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(char value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = AppendImplementation,
                Dependencies = appendDependencies,
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia("char")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(char value, int repeatCount)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) =>
                {
                    writer.AppendLine(disposed_call);
                    writer.AppendLine("WriteIndentIfNeeded();");
                    writer.AppendLine("builder.Append(value, repeatCount);");
                    writer.AppendLine("return this;");
                },
                Dependencies = appendDependencies,
                EmptyStub = return_this,
                Trivia = new TriviaSource
                {
                    Summary =
                        "Appends the specified number of copies of the specified <see cref=\"char\"/> value to the current line without creating a new line.",
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
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(char[] value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = AppendImplementation,
                Dependencies = appendDependencies,
                EmptyStub = return_this,
                Trivia = new TriviaSource
                {
                    Summary = "Appends the specified <see cref=\"char\"/> array to the current line without creating a new line.",
                    Parameters = new Dictionary<string, string>
                    {
                        ["value"] = "The char array to append."
                    },
                    Returns = APPEND_RETURN_TRIVIA
                }
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(char[] value, int startIndex, int charCount)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) =>
                {
                    writer.AppendLine(disposed_call);
                    writer.AppendLine("WriteIndentIfNeeded();");
                    writer.AppendLine("builder.Append(value, startIndex, charCount);");
                    writer.AppendLine("return this;");
                },
                Dependencies = appendDependencies,
                EmptyStub = return_this,
                Trivia = new TriviaSource
                {
                    Summary = "Appends a subarray of characters to the current line without creating a new line.",
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
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(byte value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, false),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia("byte")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(byte value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, true),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendFormatTrivia("byte")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(sbyte value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, false),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia("sbyte")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(sbyte value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, true),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendFormatTrivia("sbyte")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(short value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, false),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia("short")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(short value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, true),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendFormatTrivia("short")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(ushort value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, false),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia("ushort")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(ushort value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, true),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendFormatTrivia("ushort")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(int value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, false),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia("int")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(int value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, true),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendFormatTrivia("int")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(uint value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, false),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia("uint")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(uint value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, true),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendFormatTrivia("uint")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(long value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, false),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia("long")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(long value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, true),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendFormatTrivia("long")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(ulong value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, false),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia("ulong")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(ulong value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, true),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendFormatTrivia("ulong")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(float value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, false),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia("float")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(float value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, true),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendFormatTrivia("float")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(double value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, false),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia("double")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(double value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, true),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendFormatTrivia("double")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(decimal value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, false),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia("decimal")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(decimal value, {format_args})",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => AppendFormattable(writer, true),
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendFormatTrivia("decimal")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(bool value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => { writer.AppendLine("return Append(value ? \"true\" : \"false\");"); },
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = CreateAppendTrivia("bool")
            },
            new MethodSource
            {
                Name = "Append",
                Signature = $"public partial {GLOBAL_CODE_WRITER} Append(object value)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => { writer.AppendLine("return value == null ? this : Append(value.ToString());"); },
                Dependencies = dependsOnAppend,
                EmptyStub = return_this,
                Trivia = new TriviaSource
                {
                    Summary = "Appends the string representation of the specified object to the current line without creating a new line.",
                    Parameters = new Dictionary<string, string>
                    {
                        ["value"] = "The object to append."
                    },
                    Returns = APPEND_RETURN_TRIVIA
                }
            },
            new MethodSource
            {
                Name = "Append",
                Signature =
                    $"public partial {GLOBAL_CODE_WRITER} Append({GLOBAL_MS_CODE}.ITypeSymbol value, bool isPartial = true, bool appendNamespace = true)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) =>
                {
                    writer.AppendLine("if (appendNamespace)");
                    using (writer.WithBlock(true))
                    {
                        writer.AppendLine("AppendNamespace(value.ContainingNamespace);");
                    }

                    writer.AppendLine("Append(global::Hertzole.SourceGen.SymbolExtensions.GetDeclarationString(value, isPartial));");
                    writer.AppendLine("Append(' ');");
                    writer.AppendLine(
                        $"Append(value.ToDisplayString({GLOBAL_MS_CODE}.NullableFlowState.None, {GLOBAL_MS_CODE}.SymbolDisplayFormat.MinimallyQualifiedFormat));");

                    writer.AppendLine(return_this);
                },
                Dependencies =
                [
                    $"{CODE_WRITER}.AppendNamespace({MS_CODE}.INamespaceSymbol)",
                    $"{CODE_WRITER}.Append(string)",
                    $"{CODE_WRITER}.Append(char)",
                    $"{NAMESPACE}.SymbolExtensions.GetDeclarationString({MS_CODE}.ITypeSymbol, bool)"
                ],
                EmptyStub = return_this,
                Trivia = new TriviaSource
                {
                    Summary =
                        $"Appends the string representation of the specified {GetTypeTriviaReference($"{GLOBAL_MS_CODE}.ITypeSymbol", "ITypeSymbol", out _)}, " +
                        $"along with its required declarations, without creating a newline.<br/>\n" +
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

    private static void AppendImplementation(CodeWriter writer, in ImplementationContext ctx)
    {
        writer.AppendLine(disposed_call);
        writer.AppendLine("WriteIndentIfNeeded();");
        writer.AppendLine("builder.Append(value);");
        writer.AppendLine(return_this);
    }

    private static void AppendFormattable(CodeWriter writer, bool isImplementation)
    {
        if (isImplementation)
        {
            writer.AppendLine("return Append(value.ToString(format, provider));");
        }
        else
        {
            writer.AppendLine("return Append(value.ToString(\"G\", global::System.Globalization.CultureInfo.InvariantCulture));");
        }
    }

    private static TriviaSource CreateAppendTrivia(string type, string? displayName = null)
    {
        string typeRef = GetTypeTriviaReference(type, displayName, out string name);

        return new TriviaSource
        {
            Summary = $"Appends {typeRef} to the current line without creating a new line.",
            Parameters = new Dictionary<string, string>
            {
                ["value"] = $"The {name} to insert."
            },
            Returns = APPEND_RETURN_TRIVIA
        };
    }

    private static TriviaSource CreateAppendFormatTrivia(string type, string? displayName = null)
    {
        string typeRef = GetTypeTriviaReference(type, displayName, out string name);

        return new TriviaSource
        {
            Summary = $"Appends {typeRef} with the specified format to the current line without creating a new line.",
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