using System.Collections.Generic;
using System.Text;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private const string CODE_WRITER = NAMESPACE + ".CodeWriter";
    private const string GLOBAL_CODE_WRITER = "global::" + CODE_WRITER;

    private static TypeSource CreateCodeWriter()
    {
        const string return_this = "return this;";
        const string new_line = "builder.Append('\\n');";
        const string write_indent = CODE_WRITER + ".WriteIndentIfNeeded()";
        const string dispose = $"{CODE_WRITER}.Dispose()";
        const string throw_if_disposed = $"{CODE_WRITER}.ThrowIfDisposed()";
        const string disposed_call = "ThrowIfDisposed();";
        string[] builderDependencies =
        [
            CODE_WRITER + ".Append",
            CODE_WRITER + ".AppendLine",
            CODE_WRITER + ".AppendNamespace",
            CODE_WRITER + ".AppendNullable",
            CODE_WRITER + ".AppendGeneratedCodeAttribute",
            CODE_WRITER + ".AppendExcludeFromCodeCoverageAttribute",
            CODE_WRITER + ".AppendConditionalSymbol",
            CODE_WRITER + ".AppendPreprocessorSymbol"
        ];

        string[] appendDependencies =
        [
            write_indent,
            dispose,
            throw_if_disposed
        ];

        return new TypeSource
        {
            Signature = "internal sealed partial class CodeWriter : global::System.IDisposable",
            Trivia = new TriviaSource
            {
                Summary = "Wrapper around <c>StringBuilder</c> that provides formatting for code writing."
            },
            Fields = new Dictionary<string, FieldSource>
            {
                ["builder"] = new FieldSource
                {
                    Signature = "private global::System.Text.StringBuilder builder;",
                    Dependencies = builderDependencies
                },
                ["shouldWriteIndent"] = new FieldSource
                {
                    Signature = "private bool shouldWriteIndent = false;",
                    RequiredDependencies = [write_indent]
                },
                ["hasNamespace"] = new FieldSource
                {
                    Signature = "private bool hasNamespace = false;",
                    Dependencies = [CODE_WRITER + ".AppendNamespace(string)"]
                },
                ["hasWrittenNamespace"] = new FieldSource
                {
                    Signature = "private bool hasWrittenNamespace = false;",
                    RequiredDependencies = [CODE_WRITER + ".AppendNamespace(string)", CODE_WRITER + ".ToString()"]
                },
                ["isNullable"] = new FieldSource
                {
                    Signature = "private bool isNullable = false;",
                    RequiredDependencies = [CODE_WRITER + ".AppendNullable()", CODE_WRITER + ".ToString()"]
                },
                ["isDisposed"] = new FieldSource
                {
                    Signature = "private bool isDisposed = false;",
                    Dependencies = [CODE_WRITER + ".Dispose()", throw_if_disposed]
                }
            },
            Properties = new Dictionary<string, PropertySource>
            {
                ["Indent"] = new PropertySource
                {
                    Signature = "public int Indent",
                    Trivia = new TriviaSource
                    {
                        Summary = "The current indent. Each indent is 4 spaces/1 tab."
                    }
                }
            },
            Methods =
            [
                new MethodSource
                {
                    Name = "CodeWriter",
                    Signature = "public partial CodeWriter()",
                    Implementation = (writer, in context) =>
                    {
                        if (HasWrittenAnything(in context))
                        {
                            writer.AppendLine($"builder = global::{STRING_BUILDER_POOL}.Get();");
                        }
                    },
                    Dependencies = [$"{STRING_BUILDER_POOL}.Get()"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Creates a new instance of a code writer."
                    }
                },
                new MethodSource
                {
                    Name = "AppendNullable",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendNullable()",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in ctx) =>
                    {
                        writer.AppendLine(disposed_call);
                        writer.AppendLine("builder.Append(\"#nullable enable\");");
                        writer.AppendLine(new_line);
                        if (ctx.HasCalledMethod(NAMESPACE + ".CodeWriter.ToString()"))
                        {
                            writer.AppendLine("isNullable = true;");
                        }

                        writer.AppendLine(return_this);
                    },
                    Dependencies = [dispose, throw_if_disposed],
                    EmptyStub = return_this,
                    Trivia = new TriviaSource
                    {
                        Summary = "Appends <c>#nullable enable</c>. If appended, <c>#nullable restore</c> will be appended at the end of the file."
                    }
                },
                new MethodSource
                {
                    Name = "Append",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} Append(string value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendTrivia("string")
                },
                new MethodSource
                {
                    Name = "Append",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} Append(global::System.ReadOnlySpan<char> value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(disposed_call);
                        writer.AppendLine("WriteIndentIfNeeded();");
                        writer.AppendLine("builder.Append(value.ToString());");
                        writer.AppendLine("return this;");
                    },
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendTrivia("ReadOnlySpan<char>")
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
                        Summary = "Appends the specified number of copies of the specified value to the current line.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The value to append.",
                            ["repeatCount"] = "How many times the value should be inserted."
                        }
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
                    Trivia = CreateAppendTrivia("char[]")
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
                        Summary = "Appends a subarray of characters to the current line.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The character array to append.",
                            ["startIndex"] = "The starting position in the character array.",
                            ["charCount"] = "The number of characters to append."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "Append",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} Append(byte value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendTrivia("byte")
                },
                new MethodSource
                {
                    Name = "Append",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} Append(sbyte value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendTrivia("sbyte")
                },
                new MethodSource
                {
                    Name = "Append",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} Append(short value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendTrivia("short")
                },
                new MethodSource
                {
                    Name = "Append",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} Append(ushort value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendTrivia("ushort")
                },
                new MethodSource
                {
                    Name = "Append",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} Append(int value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendTrivia("int")
                },
                new MethodSource
                {
                    Name = "Append",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} Append(uint value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendTrivia("uint")
                },
                new MethodSource
                {
                    Name = "Append",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} Append(long value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendTrivia("long")
                },
                new MethodSource
                {
                    Name = "Append",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} Append(ulong value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendTrivia("ulong")
                },
                new MethodSource
                {
                    Name = "Append",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} Append(float value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendTrivia("float")
                },
                new MethodSource
                {
                    Name = "Append",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} Append(double value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendTrivia("double")
                },
                new MethodSource
                {
                    Name = "Append",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} Append(decimal value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendTrivia("decimal")
                },
                new MethodSource
                {
                    Name = "Append",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} Append(bool value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendTrivia("bool")
                },
                new MethodSource
                {
                    Name = "Append",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} Append(object value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendTrivia("object")
                },
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
                        Summary = "Appends a newline to the current line."
                    }
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(string value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendLineImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendLineTrivia("string")
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(global::System.ReadOnlySpan<char> value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(disposed_call);
                        writer.AppendLine("WriteIndentIfNeeded();");
                        writer.AppendLine("builder.Append(value.ToString());");
                        writer.AppendLine(new_line);
                        writer.AppendLine("shouldWriteIndent = true;");
                        writer.AppendLine(return_this);
                    },
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendLineTrivia("ReadOnlySpan&lt;char&gt;")
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
                        }
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
                        }
                    }
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(byte value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendLineImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendLineTrivia("byte")
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(sbyte value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendLineImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendLineTrivia("sbyte")
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(short value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendLineImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendLineTrivia("short")
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(ushort value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendLineImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendLineTrivia("ushort")
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(int value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendLineImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendLineTrivia("int")
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(uint value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendLineImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendLineTrivia("uint")
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(long value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendLineImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendLineTrivia("long")
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(ulong value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendLineImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendLineTrivia("ulong")
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(float value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendLineImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendLineTrivia("float")
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(double value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendLineImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendLineTrivia("double")
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(decimal value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendLineImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendLineTrivia("decimal")
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(bool value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendLineImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendLineTrivia("bool")
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendLine(object value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = AppendLineImplementation,
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = CreateAppendLineTrivia("object")
                },
                new MethodSource
                {
                    Name = "AppendNamespace",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendNamespace(global::Microsoft.CodeAnalysis.INamespaceSymbol? symbol)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(disposed_call);
                        writer.AppendLine("if (symbol == null || symbol.IsGlobalNamespace)");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine(return_this);
                        }

                        writer.AppendLine();
                        writer.AppendLine("if (hasNamespace)");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine(return_this);
                        }

                        writer.AppendLine();
                        writer.AppendLine("return AppendNamespace(symbol.ToDisplayString());");
                    },
                    Dependencies = [CODE_WRITER + ".AppendNamespace(string)", throw_if_disposed],
                    EmptyStub = return_this,
                    Trivia = new TriviaSource
                    {
                        Summary = "Appends a namespace declaration for the specified symbol. Does nothing if the symbol is null or a global namespace.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["symbol"] = "The namespace symbol to append."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "AppendNamespace",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendNamespace(string value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in ctx) =>
                    {
                        writer.AppendLine(disposed_call);
                        writer.AppendLine("if (string.IsNullOrEmpty(value))");
                        writer.AppendLine("{");
                        writer.Indent++;
                        writer.AppendLine(return_this);
                        writer.Indent--;
                        writer.AppendLine("}\n");

                        writer.AppendLine("hasNamespace = true;");
                        writer.AppendLine("builder.Append(\"namespace \");");
                        writer.AppendLine("builder.AppendLine(value);");
                        writer.AppendLine("builder.AppendLine(\"{\");");
                        writer.AppendLine("Indent++;");

                        if (ctx.HasCalledMethod(CODE_WRITER + ".ToString()"))
                        {
                            writer.AppendLine("hasWrittenNamespace = false;");
                        }

                        if (ctx.HasCalledMethod(write_indent))
                        {
                            writer.AppendLine("shouldWriteIndent = true;");
                        }

                        writer.AppendLine(return_this);
                    },
                    Dependencies = [dispose, throw_if_disposed],
                    EmptyStub = return_this,
                    Trivia = new TriviaSource
                    {
                        Summary = "Appends a namespace declaration. Does nothing if the value is null or empty.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The namespace name to append."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "AppendGeneratedCodeAttribute",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendGeneratedCodeAttribute(string generator, string version)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(disposed_call);
                        writer.AppendLine("WriteIndentIfNeeded();");
                        writer.AppendLine("builder.Append(\"[global::System.CodeDom.Compiler.GeneratedCode(\\\"\");");
                        writer.AppendLine("builder.Append(generator);");
                        writer.AppendLine("builder.Append(\"\\\", \\\"\");");
                        writer.AppendLine("builder.Append(version);");
                        writer.AppendLine("builder.Append(\"\\\")]\\n\");");
                        writer.AppendLine("shouldWriteIndent = true;");
                        writer.AppendLine(return_this);
                    },
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = new TriviaSource
                    {
                        Summary = "Appends a <c>[GeneratedCode]</c> attribute to the current line.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["generator"] = "The name of the code generator.",
                            ["version"] = "The version of the code generator."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "AppendExcludeFromCodeCoverageAttribute",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendExcludeFromCodeCoverageAttribute()",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(disposed_call);
                        writer.AppendLine("WriteIndentIfNeeded();");
                        writer.AppendLine("builder.Append(\"[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]\\n\");");
                        writer.AppendLine("shouldWriteIndent = true;");
                        writer.AppendLine(return_this);
                    },
                    Dependencies = appendDependencies,
                    EmptyStub = return_this,
                    Trivia = new TriviaSource
                    {
                        Summary = "Appends an <c>[ExcludeFromCodeCoverage]</c> attribute to the current line."
                    }
                },
                new MethodSource
                {
                    Name = "AppendConditionalSymbol",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendConditionalSymbol(string? condition)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(disposed_call);
                        writer.AppendLine("if (string.IsNullOrWhiteSpace(condition))");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine(return_this);
                        }

                        writer.AppendLine();
                        writer.AppendLine("int indent = Indent;");
                        writer.AppendLine("Indent = 0;");
                        writer.AppendLine("builder.Append('#');");
                        writer.AppendLine("builder.Append(condition);");
                        writer.AppendLine(new_line);
                        writer.AppendLine("Indent = indent;");
                        writer.AppendLine(return_this);
                    },
                    Dependencies = [dispose, throw_if_disposed],
                    EmptyStub = return_this,
                    Trivia = new TriviaSource
                    {
                        Summary = "Appends a conditional preprocessor directive (e.g. <c>#if DEBUG</c>). Does nothing if the condition is null or whitespace.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["condition"] = "The condition for the preprocessor directive."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "AppendPreprocessorSymbol",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} AppendPreprocessorSymbol(string? value)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(disposed_call);
                        writer.AppendLine("if (string.IsNullOrWhiteSpace(value))");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine(return_this);
                        }

                        writer.AppendLine();
                        writer.AppendLine("int indent = Indent;");
                        writer.AppendLine("Indent = 0;");

                        writer.AppendLine("if (value![0] != '#')");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("builder.Append('#');");
                        }

                        writer.AppendLine();
                        writer.AppendLine("builder.Append(value);");
                        writer.AppendLine(new_line);
                        writer.AppendLine("Indent = indent;");
                        writer.AppendLine(return_this);
                    },
                    Dependencies = [dispose, throw_if_disposed],
                    EmptyStub = return_this,
                    Trivia = new TriviaSource
                    {
                        Summary =
                            "Appends a preprocessor symbol. Prepends <c>#</c> if the value does not already start with it. Does nothing if the value is null or whitespace.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The preprocessor symbol to append (e.g. <c>#endif</c> or <c>endif</c>)."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "Clear",
                    Signature = $"public partial {GLOBAL_CODE_WRITER} Clear()",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in ctx) =>
                    {
                        writer.AppendLine(disposed_call);
                        writer.AppendLine("Indent = 0;");

                        if (HasWrittenAnything(in ctx))
                        {
                            writer.AppendLine("builder.Clear();");
                            writer.AppendLine("shouldWriteIndent = false;");

                            bool hasCalledToString = ctx.HasCalledMethod(CODE_WRITER + ".ToString()");

                            if (ctx.HasCalledMethod(CODE_WRITER + ".AppendNamespace(string)") && hasCalledToString)
                            {
                                writer.AppendLine("hasWrittenNamespace = false;");
                                writer.AppendLine("hasNamespace = false;");
                            }

                            if (ctx.HasCalledMethod(CODE_WRITER + ".AppendNullable()") && hasCalledToString)
                            {
                                writer.AppendLine("isNullable = false;");
                            }
                        }

                        writer.AppendLine(return_this);
                    },
                    Dependencies = [dispose, throw_if_disposed],
                    EmptyStub = return_this,
                    Trivia = new TriviaSource
                    {
                        Summary = "Clears all written content and resets the writer state."
                    }
                },
                new MethodSource
                {
                    Name = "WriteIndentIfNeeded",
                    Signature = "private void WriteIndentIfNeeded()",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("if (!shouldWriteIndent)");
                        writer.AppendLine("{");
                        writer.Indent++;
                        writer.AppendLine("return;");
                        writer.Indent--;
                        writer.AppendLine("}");
                        writer.AppendLine();
                        writer.AppendLine("shouldWriteIndent = false;");
                        writer.AppendLine("builder.Append(' ', Indent * 4);");
                    },
                    SkipPartial = true,
                    Trivia = new TriviaSource
                    {
                        Summary = "Writes indentation to the builder if needed."
                    }
                },
                new MethodSource
                {
                    Name = "ToString",
                    Signature = "public override partial string ToString()",
                    EmptyStub = "return string.Empty;",
                    Implementation = (writer, in ctx) =>
                    {
                        writer.AppendLine(disposed_call);
                        if (!HasWrittenAnything(in ctx))
                        {
                            writer.AppendLine("return string.Empty;");
                            return;
                        }

                        writer.AppendLine("if (builder.Length == 0)");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("return string.Empty;");
                        }

                        writer.AppendLine();

                        if (ctx.HasCalledMethod(CODE_WRITER + ".AppendNamespace(string)"))
                        {
                            writer.AppendLine("if (hasNamespace && !hasWrittenNamespace)");
                            using (writer.WithBlock())
                            {
                                writer.AppendLine(new_line);
                                writer.AppendLine("Indent--;");
                                writer.AppendLine("builder.Append(\"}\\n\");");
                                writer.AppendLine("hasWrittenNamespace = true;");
                                writer.AppendLine("hasNamespace = false;");
                            }

                            writer.AppendLine();
                        }

                        if (ctx.HasCalledMethod(NAMESPACE + ".CodeWriter.AppendNullable()"))
                        {
                            writer.AppendLine("if (isNullable)");
                            using (writer.WithBlock())
                            {
                                writer.AppendLine("builder.Append(\"#nullable restore\\n\");");
                            }

                            writer.AppendLine();
                        }

                        writer.AppendLine("// Trim the last newline, if present.");
                        writer.AppendLine("if (builder[builder.Length - 1] == '\\n')");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("builder.Remove(builder.Length - 1, 1);");
                        }

                        writer.AppendLine();

                        writer.AppendLine("return builder.ToString();");
                    },
                    Dependencies = [throw_if_disposed],
                    Trivia = new TriviaSource
                    {
                        Summary = "Returns the written content as a string. Closes any open namespace block and appends <c>#nullable restore</c> if needed."
                    }
                },
                new MethodSource
                {
                    Name = "Dispose",
                    Signature = "public partial void Dispose()",
                    Implementation = (writer, in context) =>
                    {
                        if (HasWrittenAnything(in context))
                        {
                            writer.AppendLine($"global::{STRING_BUILDER_POOL}.Return(builder);");
                        }

                        writer.AppendLine("isDisposed = true;");
                    },
                    Dependencies = [$"{STRING_BUILDER_POOL}.Return(System.Text.StringBuilder)"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Disposes the code writer and returns the underlying <c>StringBuilder</c> to the pool."
                    }
                },
                new MethodSource
                {
                    Name = "ThrowIfDisposed",
                    Signature = "private void ThrowIfDisposed()",
                    SkipPartial = true,
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("if (!isDisposed)");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("return;");
                        }

                        writer.AppendLine();
                        writer.AppendLine("throw new global::System.ObjectDisposedException(\"CodeWriter\", \"The code writer has been disposed.\");");
                    },
                    Trivia = new TriviaSource
                    {
                        Summary = "Throws an <see cref=\"global::System.ObjectDisposedException\"/> if the writer has been disposed."
                    }
                },
                new MethodSource
                {
                    Name = "AsStringBuilder",
                    Signature = "public partial global::System.Text.StringBuilder AsStringBuilder()",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in ctx) =>
                    {
                        if (!HasWrittenAnything(in ctx))
                        {
                            writer.AppendLine("return new global::System.Text.StringBuilder();");
                            return;
                        }

                        writer.AppendLine("return builder;");
                    },
                    EmptyStub = "return null!;",
                    Trivia = new TriviaSource
                    {
                        Summary =
                            "Returns the underlying <c>StringBuilder</c>. The caller takes ownership of the builder and this may mutate the <c>CodeWriter</c>."
                    }
                },
                new MethodSource
                {
                    Name = "WithBlock",
                    Signature = "public partial global::" + NAMESPACE + ".CodeWriter.BlockScope WithBlock()",
                    EmptyStub = "return default;",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(disposed_call);
                        writer.AppendLine($"return new global::{NAMESPACE}.CodeWriter.BlockScope(this);");
                    },
                    Dependencies = [CODE_WRITER + ".BlockScope.BlockScope(Hertzole.SourceGen.CodeWriter)", throw_if_disposed],
                    Trivia = new TriviaSource
                    {
                        Summary = "Opens a new code block. Returns a disposable scope that closes the block and restores indentation when disposed."
                    }
                },
                new MethodSource
                {
                    Name = "WithIndent",
                    Signature = "public partial global::" + NAMESPACE + ".CodeWriter.IndentScope WithIndent(int newIndent)",
                    EmptyStub = "return default;",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(disposed_call);
                        writer.AppendLine($"return new global::{NAMESPACE}.CodeWriter.IndentScope(this, newIndent);");
                    },
                    Dependencies = [CODE_WRITER + ".IndentScope.IndentScope(Hertzole.SourceGen.CodeWriter, int)", throw_if_disposed],
                    Trivia = new TriviaSource
                    {
                        Summary = "Temporarily changes the indentation level. Returns a disposable scope that restores the original indentation when disposed.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["newIndent"] = "The indentation level to use within the scope."
                        }
                    }
                }
            ],
            Types = new Dictionary<string, TypeSource>
            {
                ["BlockScope"] = new TypeSource
                {
                    Signature = "internal readonly partial struct BlockScope : global::System.IDisposable",
                    Trivia = new TriviaSource
                    {
                        Summary = "Disposable scope that manages code block indentation. Opens a block on creation and closes it on disposal."
                    },
                    Fields = new Dictionary<string, FieldSource>
                    {
                        ["writer"] = new FieldSource
                        {
                            Signature = $"private readonly global::{NAMESPACE}.CodeWriter writer;",
                            Dependencies = [CODE_WRITER + ".BlockScope.BlockScope"]
                        }
                    },
                    Methods =
                    [
                        new MethodSource
                        {
                            Name = "BlockScope",
                            Signature = $"public partial BlockScope(global::{NAMESPACE}.CodeWriter writer)",
                            Implementation = (writer, in _) =>
                            {
                                writer.AppendLine("this.writer = writer;");
                                writer.AppendLine("writer.AppendLine('{');");
                                writer.AppendLine("writer.Indent++;");
                            },
                            Dependencies = [CODE_WRITER + ".BlockScope.Dispose()", CODE_WRITER + ".AppendLine(char)"],
                            Trivia = new TriviaSource
                            {
                                Summary = "Creates a new block scope that opens a code block and increments the indentation.",
                                Parameters = new Dictionary<string, string>
                                {
                                    ["writer"] = "The code writer to write to."
                                }
                            }
                        },
                        new MethodSource
                        {
                            Name = "Dispose",
                            Signature = "public partial void Dispose()",
                            Implementation = (writer, in _) =>
                            {
                                writer.AppendLine("writer.Indent--;");
                                writer.AppendLine("writer.AppendLine('}');");
                            },
                            Dependencies = [CODE_WRITER + ".AppendLine(char)"],
                            Trivia = new TriviaSource
                            {
                                Summary = "Closes the code block and decrements the indentation."
                            }
                        }
                    ]
                },
                ["IndentScope"] = new TypeSource
                {
                    Signature = "internal readonly partial struct IndentScope : global::System.IDisposable",
                    Trivia = new TriviaSource
                    {
                        Summary = "Disposable scope that temporarily changes the indentation level. Restores the original indentation on disposal."
                    },
                    Fields = new Dictionary<string, FieldSource>
                    {
                        ["writer"] = new FieldSource
                        {
                            Signature = $"private readonly global::{NAMESPACE}.CodeWriter writer;",
                            Dependencies = [CODE_WRITER + ".IndentScope.IndentScope"]
                        },
                        ["originalIndent"] = new FieldSource
                        {
                            Signature = "private readonly int originalIndent;",
                            Dependencies = [CODE_WRITER + ".IndentScope.IndentScope"]
                        }
                    },
                    Methods =
                    [
                        new MethodSource
                        {
                            Name = "IndentScope",
                            Signature = $"public partial IndentScope(global::{NAMESPACE}.CodeWriter writer, int newIndent)",
                            Implementation = (writer, in _) =>
                            {
                                writer.AppendLine("this.writer = writer;");
                                writer.AppendLine("originalIndent = writer.Indent;");
                                writer.AppendLine("writer.Indent = newIndent;");
                            },
                            Dependencies = [CODE_WRITER + ".IndentScope.Dispose()"],
                            Trivia = new TriviaSource
                            {
                                Summary = "Creates a new indent scope that temporarily changes the indentation level.",
                                Parameters = new Dictionary<string, string>
                                {
                                    ["writer"] = "The code writer to modify.",
                                    ["newIndent"] = "The indentation level to use within the scope."
                                }
                            }
                        },
                        new MethodSource
                        {
                            Name = "Dispose",
                            Signature = "public partial void Dispose()",
                            Implementation = (writer, in _) => { writer.AppendLine("writer.Indent = originalIndent;"); },
                            Trivia = new TriviaSource
                            {
                                Summary = "Restores the original indentation level."
                            }
                        }
                    ]
                }
            }
        };

        static void AppendImplementation(CodeWriter writer, in ImplementationContext ctx)
        {
            writer.AppendLine(disposed_call);
            writer.AppendLine("WriteIndentIfNeeded();");
            writer.AppendLine("builder.Append(value);");
            writer.AppendLine(return_this);
        }

        static void AppendLineImplementation(CodeWriter writer, in ImplementationContext ctx)
        {
            writer.AppendLine(disposed_call);
            writer.AppendLine("WriteIndentIfNeeded();");
            writer.AppendLine("builder.Append(value);");
            writer.AppendLine(new_line);
            writer.AppendLine("shouldWriteIndent = true;");
            writer.AppendLine(return_this);
        }

        static bool HasWrittenAnything(in ImplementationContext ctx)
        {
            return ctx.HasCalledMethod(CODE_WRITER + ".Append") ||
                   ctx.HasCalledMethod(CODE_WRITER + ".AppendLine") ||
                   ctx.HasCalledMethod(CODE_WRITER + ".AppendNullable()") ||
                   ctx.HasCalledMethod(CODE_WRITER + ".AppendNamespace(string)") ||
                   ctx.HasCalledMethod(CODE_WRITER + ".AppendGeneratedCodeAttribute(string, string)") ||
                   ctx.HasCalledMethod(CODE_WRITER + ".AppendExcludeFromCodeCoverageAttribute()") ||
                   ctx.HasCalledMethod(CODE_WRITER + ".AppendConditionalSymbol") ||
                   ctx.HasCalledMethod(CODE_WRITER + ".AppendPreprocessorSymbol");
        }

        static TriviaSource CreateAppendTrivia(string type)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(type).Replace("<", "&lt;").Replace(">", "&gt;");

            return new TriviaSource
            {
                Summary = $"Appends <c>{sb}</c> to the current line.",
                Parameters = new Dictionary<string, string>
                {
                    ["value"] = "The value to insert."
                }
            };
        }

        static TriviaSource CreateAppendLineTrivia(string type)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(type).Replace("<", "&lt;").Replace(">", "&gt;");

            return new TriviaSource
            {
                Summary = $"Appends <c>{sb}</c> followed by a newline to the current line.",
                Parameters = new Dictionary<string, string>
                {
                    ["value"] = "The value to insert."
                }
            };
        }
    }
}