using System.Collections.Generic;

namespace Hertzole.SourceGenUtils.Helpers;

internal static partial class CodeWriterGenerator
{
    private static readonly string[] builderDependencies =
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

    private static readonly string[] appendDependencies =
    [
        write_indent,
        dispose,
        throw_if_disposed
    ];

    private static readonly string[] dependsOnAppend = [CODE_WRITER + ".Append(string)"];
    private static readonly string[] dependsOnAppendLine = [CODE_WRITER + ".AppendLine(string)"];
    private const string return_this = "return this;";
    private const string disposed_call = "ThrowIfDisposed();";
    private const string dispose = $"{CODE_WRITER}.Dispose()";
    private const string throw_if_disposed = $"{CODE_WRITER}.ThrowIfDisposed()";
    private const string write_indent = CODE_WRITER + ".WriteIndentIfNeeded()";
    private const string new_line = "builder.Append('\\n');";
    private const string format_args = "string format, global::System.IFormatProvider? provider = null";

    public static TypeSource CreateCodeWriter()
    {
        return new TypeSource
        {
            Signature = "internal sealed partial class CodeWriter : global::System.IDisposable",
            Trivia = new TriviaSource
            {
                Summary =
                    "Wrapper around <see cref=\"global::System.Text.StringBuilder\">StringBuilder</see> that provides formatting and utility methods for code writing."
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
            Methods = GetMethods(),
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
                },
                ["ConditionalScope"] = new TypeSource
                {
                    Signature = "internal readonly partial struct ConditionalScope : global::System.IDisposable",
                    Trivia = new TriviaSource
                    {
                        Summary = "Disposable scope that appends <c>#if CONDITION</c>. Appends <c>#endif</c> on disposal."
                    },
                    Fields = new Dictionary<string, FieldSource>
                    {
                        ["writer"] = new FieldSource
                        {
                            Signature = $"private readonly global::{NAMESPACE}.CodeWriter writer;",
                            Dependencies = [CODE_WRITER + ".ConditionalScope.ConditionalScope"]
                        }
                    },
                    Methods =
                    [
                        new MethodSource
                        {
                            Name = "ConditionalScope",
                            Signature = $"public partial ConditionalScope(global::{NAMESPACE}.CodeWriter writer, string? condition)",
                            Implementation = (writer, in _) =>
                            {
                                writer.AppendLine("this.writer = writer;");
                                writer.AppendLine("writer.AppendConditionalSymbol(condition);");
                            },
                            Dependencies = [CODE_WRITER + ".ConditionalScope.Dispose()", CODE_WRITER + ".AppendConditionalSymbol(string)"],
                            Trivia = new TriviaSource
                            {
                                Summary = "Creates a new conditional scope.",
                                Parameters = new Dictionary<string, string>
                                {
                                    ["writer"] = "The code writer to modify.",
                                    ["condition"] = "The condition to append."
                                }
                            }
                        },
                        new MethodSource
                        {
                            Name = "Dispose",
                            Signature = "public partial void Dispose()",
                            Implementation = (writer, in _) => { writer.AppendLine("writer.AppendPreprocessorSymbol(\"#endif\");"); },
                            Dependencies = [CODE_WRITER + ".AppendPreprocessorSymbol(string)"],
                            Trivia = new TriviaSource
                            {
                                Summary = "Closes the conditional."
                            }
                        }
                    ]
                }
            }
        };
    }

    private static MethodSource[] GetMethods()
    {
        return
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
                    Summary = $"Creates a new instance of a {GetTypeTriviaReference(GLOBAL_CODE_WRITER, "CodeWriter", out _)}."
                }
            },
            .. GetAppendMethods(),
            .. GetAppendLineMethods(),
            .. GetAppendMiscMethods(),
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
                    Summary = "Clears all written content and resets the writer state.",
                    Returns = APPEND_RETURN_TRIVIA
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
                            writer.AppendLine("if (builder[builder.Length - 1] != '\\n')");
                            using (writer.WithBlock(true))
                            {
                                writer.AppendLine(new_line);
                            }

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
                    Summary = "Returns the written content as a string. Closes any open namespace block and appends <c>#nullable restore</c> if needed.",
                    Returns = "The written content as a string."
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
                    Summary =
                        $"Disposes the code writer and returns the underlying {GetTypeTriviaReference("global::System.Text.StringBuilder", "StringBuilder", out _)} to the pool."
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
                    Summary = "Opens a new code block. Returns a disposable scope that closes the block and restores indentation when disposed.",
                    Returns = "A disposable scope that closes the block when disposed.",
                    Example = """
                              <code>
                              using CodeWriter writer = new CodeWriter();

                              writer.AppendLine("public class MyClass");
                              using (writer.WithBlock())
                              {
                                  writer.AppendLine("public int myField;");
                              }

                              return writer.ToString();
                              </code>

                              Output:
                              <code>
                              public class MyClass
                              {
                                  public int myField;
                              }
                              </code>
                              """
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
                    },
                    Returns = "A disposable scope that restores the original indentation when disposed.",
                    Example = """
                              <code>
                              using CodeWriter writer = new CodeWriter();

                              writer.AppendLine("if (true == false)
                              using (writer.WithIndent(1))
                              {
                                  writer.AppendLine("return false;");
                              }
                              return writer.ToString();
                              </code>

                              Output:
                              <code>
                              if (true == false)
                                  return false;
                              </code>
                              """
                }
            },
            new MethodSource
            {
                Name = "WithCondition",
                Signature = "public partial global::" + NAMESPACE + ".CodeWriter.ConditionalScope WithCondition(string? condition)",
                EmptyStub = "return default;",
                Implementation = (writer, in _) =>
                {
                    writer.AppendLine(disposed_call);
                    writer.AppendLine($"return new global::{NAMESPACE}.CodeWriter.ConditionalScope(this, condition);");
                },
                Dependencies = [CODE_WRITER + ".ConditionalScope.ConditionalScope(Hertzole.SourceGen.CodeWriter, string)", throw_if_disposed],
                Trivia = new TriviaSource
                {
                    Summary =
                        "Creates a scope in a preprocessor conditional block. Returns a disposable scope that closes the conditional block when disposed.",
                    Parameters = new Dictionary<string, string>
                    {
                        ["condition"] = "The condition."
                    },
                    Returns = "A disposable scope that closes the conditional block when disposed.",
                    Example = """
                              <code>
                              using CodeWriter writer = new CodeWriter();

                              using (writer.WithCondition("DEBUG"))
                              {
                                  writer.AppendLine("Log(\"This is a debug message\");");
                              }
                              </code>

                              Output:
                              <code>
                              #if DEBUG
                              Log("This is a debug message");
                              #endif
                              </code>
                              """
                }
            }
        ];
    }

    private static bool HasWrittenAnything(in ImplementationContext ctx)
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
}