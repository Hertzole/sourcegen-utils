using System.Collections.Generic;

namespace Hertzole.SourceGenUtils.Helpers;

internal partial class CodeWriterGenerator
{
    public static MethodSource[] GetAppendMiscMethods()
    {
        TriviaSource appendIndentedSourceTrivia = new TriviaSource
        {
            Summary = "Appends an entire multi-line string to the writer and handles the indentation.",
            Remarks =
                "This method automatically indents each line of the string according to the current indentation level. It handles replacing newlines with the current line ending and tabs with spaces.",
            Example = """"
                      <code>
                      using CodeWriter writer = new CodeWriter();
                      string value = """
                                     if (true == false)
                                     {
                                         // Do thing
                                     }
                                     """

                      writer.AppendLine("public void MyMethod()");
                      using (writer.WithBlock())
                      {
                          // Appended within an indented block
                          writer.AppendIndentedSource(value);
                      }

                      writer.ToString();
                      </code>

                      The result of calling <c>ToString()</c>:
                      <code>
                      public void MyMethod()
                      {
                          if (true == false)
                          {
                              // Do thing
                          }
                      }
                      </code>
                      """",
            Returns = APPEND_RETURN_TRIVIA,
            Parameters = new Dictionary<string, string>
            {
                { "value", "The value to append." }
            }
        };

        return
        [
            new MethodSource
            {
                Name = "AppendIndentedSource",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendIndentedSource(string value)",
                Implementation = (writer, in _) => { writer.AppendLine($"return AppendIndentedSource({GLOBAL_MEMORY_EXT}.AsSpan(value));"); },
                EmptyStub = return_this,
                Dependencies = [$"{CODE_WRITER}.AppendIndentedSource({R_SPAN}<char>)"],
                Attributes = AggressiveInlineAttribute,
                Trivia = appendIndentedSourceTrivia
            },
            new MethodSource
            {
                Name = "AppendIndentedSource",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendIndentedSource({GLOBAL_R_SPAN}<char> value)",
                Implementation = (writer, in _) =>
                {
                    writer.AppendIndentedSource($$"""
                                                  {{disposed_call}}
                                                  WriteIndentIfNeeded();

                                                  using ({{GLOBAL_STRING_BUILDER_POOL}}.Get(out global::System.Text.StringBuilder tempBuilder))
                                                  {
                                                      {{GLOBAL_STRING_BUILDER_EXTENSIONS}}.Append(tempBuilder, value);
                                                      if ({{GLOBAL_MEMORY_EXT}}.Contains(value, {{GLOBAL_MEMORY_EXT}}.AsSpan("\t"), global::System.StringComparison.Ordinal))
                                                      {
                                                          tempBuilder.Replace("\t", new string(' ', Indent * 4));
                                                      }
                                                      
                                                      if ({{GLOBAL_MEMORY_EXT}}.Contains(value, {{GLOBAL_MEMORY_EXT}}.AsSpan("\r\n"), global::System.StringComparison.Ordinal))
                                                      {
                                                          tempBuilder.Replace("\r\n", "\n");
                                                      }
                                                      
                                                      char[] buffer = {{GLOBAL_ARRAY_POOL}}<char>.Shared.Rent(value.Length);
                                                      tempBuilder.CopyTo(0, buffer, 0, tempBuilder.Length);
                                                      
                                                      value = {{GLOBAL_MEMORY_EXT}}.Trim({{GLOBAL_MEMORY_EXT}}.AsSpan(buffer, 0, tempBuilder.Length));

                                                      builder.EnsureCapacity(builder.Length + value.Length);
                                                      
                                                      int offset = 0;
                                                      int index;
                                                      while ((index = {{GLOBAL_MEMORY_EXT}}.IndexOf(value.Slice(offset), '\n')) != -1)
                                                      {
                                                          if (offset != 0 && !{{GLOBAL_MEMORY_EXT}}.IsWhiteSpace(value.Slice(offset, index)))
                                                          {
                                                              builder.Append(' ', Indent * 4);
                                                          }
                                                      
                                                          Append(value.Slice(offset, index));
                                                          Append('\n');
                                                          
                                                          offset += index + 1;
                                                      }
                                                      
                                                      builder.Append(' ', Indent * 4);
                                                      Append(value.Slice(offset));
                                                      AppendLine();
                                                      
                                                      shouldWriteIndent = true;
                                                      
                                                      {{return_this}}
                                                  }
                                                  """);
                },
                Dependencies =
                [
                    write_indent,
                    $"{CODE_WRITER}.Append({R_SPAN}<char>)",
                    $"{CODE_WRITER}.Append(char)",
                    $"{CODE_WRITER}.AppendLine()",
                    $"{STRING_BUILDER_POOL}.Get(System.Text.StringBuilder)"
                ],
                Trivia = appendIndentedSourceTrivia,
                EmptyStub = return_this
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
                    Summary = "Appends <c>#nullable enable</c>. If appended, <c>#nullable restore</c> will be appended at the end of the file.",
                    Returns = APPEND_RETURN_TRIVIA
                }
            },
            new MethodSource
            {
                Name = "AppendNamespace",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendNamespace({GLOBAL_MS_CODE}.INamespaceSymbol? symbol)",
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
                    },
                    Returns = APPEND_RETURN_TRIVIA
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
                    },
                    Returns = APPEND_RETURN_TRIVIA
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
                    Summary =
                        $"Appends a {GetTypeTriviaReference("global::System.CodeDom.Compiler.GeneratedCodeAttribute", "GeneratedCode", out _)} attribute to the current line, followed by a newline.",
                    Parameters = new Dictionary<string, string>
                    {
                        ["generator"] = "The name of the code generator.",
                        ["version"] = "The version of the code generator."
                    },
                    Returns = APPEND_RETURN_TRIVIA
                }
            },
            new MethodSource
            {
                Name = "AppendExcludeFromCodeCoverageAttribute",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendExcludeFromCodeCoverageAttribute()",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) =>
                {
                    writer.AppendLine("return AppendLine(\"[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]\");");
                },
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = new TriviaSource
                {
                    Summary =
                        $"Appends an {GetTypeTriviaReference("global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute", "ExcludeFromCodeCoverage", out _)} attribute to the current line, followed by a newline.",
                    Returns = APPEND_RETURN_TRIVIA
                }
            },
            new MethodSource
            {
                Name = "AppendEmbeddedAttribute",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendEmbeddedAttribute()",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) => { writer.AppendLine($"return AppendLine(\"[{GLOBAL_MS_CODE}.EmbeddedAttribute]\");"); },
                Dependencies = dependsOnAppendLine,
                EmptyStub = return_this,
                Trivia = new TriviaSource
                {
                    Summary =
                        $"Appends an <c>[Embedded]</c> attribute to the current line. You should only use this if you've added <c>{MS_CODE}.EmbeddedAttribute</c>.",
                    Returns = APPEND_RETURN_TRIVIA
                }
            },
            new MethodSource
            {
                Name = "AppendConditionalSymbol",
                Signature = $"public partial {GLOBAL_CODE_WRITER} AppendConditionalSymbol(string? condition)",
                Attributes = AggressiveInlineAttribute,
                Implementation = (writer, in _) =>
                {
                    const string memory_ext = "global::System.MemoryExtensions";

                    writer.AppendLine(disposed_call);
                    writer.AppendLine("if (string.IsNullOrWhiteSpace(condition))");
                    using (writer.WithBlock(true))
                    {
                        writer.AppendLine(return_this);
                    }

                    writer.AppendLine($"global::System.ReadOnlySpan<char> span = {memory_ext}.Trim({memory_ext}.AsSpan(condition));");
                    writer.AppendLine("int indent = Indent;");
                    writer.AppendLine("Indent = 0;");

                    writer.AppendLine($"if ({memory_ext}.StartsWith(span, \"if \"))");
                    using (writer.WithBlock())
                    {
                        writer.AppendLine("builder.Append('#');");
                    }

                    writer.AppendLine($"else if (!{memory_ext}.StartsWith(span, \"#if \"))");
                    using (writer.WithBlock(true))
                    {
                        writer.AppendLine("builder.Append(\"#if \");");
                    }

                    writer.AppendLine("Append(span);");
                    writer.AppendLine(new_line);
                    writer.AppendLine("Indent = indent;");
                    writer.AppendLine(return_this);
                },
                Dependencies = [dispose, throw_if_disposed, CODE_WRITER + ".Append(System.ReadOnlySpan<char>)"],
                EmptyStub = return_this,
                Trivia = new TriviaSource
                {
                    Summary = "Appends a conditional preprocessor directive (e.g. <c>#if DEBUG</c>). Does nothing if the condition is null or whitespace.",
                    Parameters = new Dictionary<string, string>
                    {
                        ["condition"] = "The condition for the preprocessor directive."
                    },
                    Returns = APPEND_RETURN_TRIVIA
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
                    },
                    Returns = APPEND_RETURN_TRIVIA
                }
            }
        ];
    }
}