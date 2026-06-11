using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private static TypeSource CreateCodeWriter()
    {
        return new TypeSource
        {
            Signature = "internal sealed partial class CodeWriter",
            Fields = new Dictionary<string, FieldSource>
            {
                ["builder"] = new FieldSource
                {
                    Signature = "private global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder(1024);",
                    Dependencies = ["CodeWriter.Append", "CodeWriter.AppendLine", "CodeWriter.AppendNamespace", "CodeWriter.AppendNullable"]
                },
                ["shouldWriteIndent"] = new FieldSource
                {
                    Signature = "private bool shouldWriteIndent = false;",
                    Dependencies = ["CodeWriter.WriteIndentIfNeeded", "CodeWriter.AppendLine"]
                },
                ["hasNamespace"] = new FieldSource
                {
                    Signature = "private bool hasNamespace = false;",
                    Dependencies = ["CodeWriter.AppendNamespace", "CodeWriter.ToString"]
                },
                ["hasWrittenNamespace"] = new FieldSource
                {
                    Signature = "private bool hasWrittenNamespace = false;",
                    Dependencies = ["CodeWriter.AppendNamespace", "CodeWriter.ToString"]
                },
                ["isNullable"] = new FieldSource
                {
                    Signature = "private bool isNullable = false;",
                    Dependencies = ["CodeWriter.AppendNullable", "CodeWriter.ToString"]
                }
            },
            Properties = new Dictionary<string, PropertySource>
            {
                ["Indent"] = new PropertySource
                {
                    Signature = "public int Indent { get; set; }"
                }
            },
            Methods =
            [
                new MethodSource
                {
                    Name = "AppendNullable",
                    Signature = "public partial void AppendNullable()",
                    Implementation = writer =>
                    {
                        writer.AppendLine("builder.AppendLine(\"#nullable enable\");");
                        writer.AppendLine("isNullable = true;");
                    }
                },
                new MethodSource
                {
                    Name = "Append",
                    Signature = "public partial void Append(string value)",
                    Implementation = AppendImplementation,
                    Dependencies = ["CodeWriter.WriteIndentIfNeeded()"]
                },
                new MethodSource
                {
                    Name = "Append",
                    Signature = "public partial void Append(char value)",
                    Implementation = AppendImplementation,
                    Dependencies = ["CodeWriter.WriteIndentIfNeeded()"]
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = "public partial void AppendLine()",
                    Implementation = writer =>
                    {
                        writer.AppendLine("builder.AppendLine();");
                        writer.AppendLine("shouldWriteIndent = true;");
                    }
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = "public partial void AppendLine(string value)",
                    Implementation = writer =>
                    {
                        writer.AppendLine("WriteIndentIfNeeded();");
                        writer.AppendLine("builder.AppendLine(value);");
                        writer.AppendLine("shouldWriteIndent = true;");
                    },
                    Dependencies = ["CodeWriter.WriteIndentIfNeeded()"]
                },
                new MethodSource
                {
                    Name = "AppendNamespace",
                    Signature = "public partial void AppendNamespace(global::Microsoft.CodeAnalysis.INamespaceSymbol? symbol)",
                    Implementation = writer =>
                    {
                        writer.AppendLine("if (symbol == null || symbol.IsGlobalNamespace)");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("return;");
                        }

                        writer.AppendLine();
                        writer.AppendLine("if (hasNamespace)");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("return;");
                        }

                        writer.AppendLine();
                        writer.AppendLine("AppendNamespace(symbol.ToDisplayString());");
                    },
                    Dependencies = ["CodeWriter.AppendNamespace(string)"]
                },
                new MethodSource
                {
                    Name = "AppendNamespace",
                    Signature = "public partial void AppendNamespace(string value)",
                    Implementation = writer =>
                    {
                        writer.AppendLine("if (string.IsNullOrEmpty(value))");
                        writer.AppendLine("{");
                        writer.Indent++;
                        writer.AppendLine("return;");
                        writer.Indent--;
                        writer.AppendLine("}\n");

                        writer.AppendLine("hasNamespace = true;");
                        writer.AppendLine("hasWrittenNamespace = false;");
                        writer.AppendLine("builder.Append(\"namespace \");");
                        writer.AppendLine("builder.AppendLine(value);");
                        writer.AppendLine("builder.AppendLine(\"{\");");
                        writer.AppendLine("Indent++;");
                        writer.AppendLine("shouldWriteIndent = true;");
                    },
                    Dependencies = ["CodeWriter.WriteIndentIfNeeded()"]
                },
                new MethodSource
                {
                    Name = "WriteIndentIfNeeded",
                    Signature = "private void WriteIndentIfNeeded()",
                    Implementation = writer =>
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
                    SkipPartial = true
                },
                new MethodSource
                {
                    Name = "ToString",
                    Signature = "public override partial string ToString()",
                    EmptyStub = "return string.Empty;",
                    Implementation = writer =>
                    {
                        writer.AppendLine("if (hasNamespace && !hasWrittenNamespace)");
                        writer.AppendLine("{");
                        writer.Indent++;
                        writer.AppendLine("builder.AppendLine();");
                        writer.AppendLine("Indent--;");
                        writer.AppendLine("builder.AppendLine(\"}\");");
                        writer.AppendLine("hasWrittenNamespace = true;");
                        writer.AppendLine("hasNamespace = false;");
                        writer.Indent--;
                        writer.AppendLine("}");

                        writer.AppendLine();
                        writer.AppendLine("if (isNullable)");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("builder.AppendLine(\"#nullable restore\");");
                        }

                        writer.AppendLine();
                        writer.AppendLine("return builder.ToString();");
                    }
                },
                new MethodSource
                {
                    Name = "WithBlock",
                    Signature = "public partial global::" + NAMESPACE + ".CodeWriter.BlockScope WithBlock()",
                    EmptyStub = "return default;",
                    Implementation = writer => { writer.AppendLine($"return new global::{NAMESPACE}.CodeWriter.BlockScope(this);"); },
                    Dependencies = ["CodeWriter.BlockScope.BlockScope(Hertzole.SourceGen.CodeWriter)"]
                }
            ],
            Types = new Dictionary<string, TypeSource>
            {
                ["BlockScope"] = new TypeSource
                {
                    Signature = "internal readonly struct BlockScope : global::System.IDisposable",
                    Fields = new Dictionary<string, FieldSource>
                    {
                        ["writer"] = new FieldSource
                        {
                            Signature = $"private readonly global::{NAMESPACE}.CodeWriter writer;",
                            Dependencies = ["CodeWriter.WithBlock"]
                        }
                    },
                    Methods =
                    [
                        new MethodSource
                        {
                            Name = "BlockScope",
                            Signature = $"public BlockScope(global::{NAMESPACE}.CodeWriter writer)",
                            Implementation = writer =>
                            {
                                writer.AppendLine("this.writer = writer;");
                                writer.AppendLine("writer.AppendLine(\"{\");");
                                writer.AppendLine("writer.Indent++;");
                            },
                            Dependencies = ["CodeWriter.BlockScope.Dispose()"]
                        },
                        new MethodSource
                        {
                            Name = "Dispose",
                            Signature = "public void Dispose()",
                            Implementation = writer =>
                            {
                                writer.AppendLine("writer.Indent--;");
                                writer.AppendLine("writer.AppendLine(\"}\");");
                            },
                            Dependencies = ["CodeWriter.WithBlock"]
                        }
                    ]
                }
            }
        };
    }

    private static void AppendImplementation(CodeWriter writer)
    {
        writer.AppendLine("WriteIndentIfNeeded();\n");
        writer.AppendLine("builder.Append(value);");
    }
}