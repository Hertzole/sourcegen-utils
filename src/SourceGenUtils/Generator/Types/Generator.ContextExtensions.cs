using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private const string CONTEXT_EXTENSIONS_EXAMPLE =
        """
        The following example demonstrates how to add generated source code to the compilation from an incremental generator.

        <code>
        using Hertzole.SourceGen;
        using Microsoft.CodeAnalysis;

        public class MyGenerator : IIncrementalGenerator
        {
            public void Initialize(IncrementalGeneratorInitializationContext context)
            {
                context.RegisterSourceOutput(context.CompilationProvider, (productionContext, _) =>
                {
                    // Must dispose when the writer is no longer needed.
                    using CodeWriter writer = new CodeWriter();
                    writer.AppendLine("namespace My.Namespace");
                    using (writer.WithBlock())
                    {
                        writer.AppendLine("public class GeneratedClass { }");
                    }

                    // Adds the writer's contents to the compilation and clears the writer.
                    productionContext.AddSource("My.Namespace.GeneratedClass.g.cs", writer);
                });
            }
        }
        </code>

        The following example adds source code using a specific encoding instead of the default UTF-8.

        <code>
        using System.Text;
        using Hertzole.SourceGen;

        public static class SourceEmitter
        {
            public static void Emit(SourceProductionContext context)
            {
                using CodeWriter writer = new CodeWriter();
                writer.AppendLine("// Generated code.");

                context.AddSource("Generated.g.cs", writer, Encoding.UTF8);
            }
        }
        </code>
        """;

    private static TypeSource CreateContextExtensions()
    {
        const string ms_analysis = "global::Microsoft.CodeAnalysis";
        const string source_text = $"{ms_analysis}.Text.SourceText";
        const string encoding = "System.Text.Encoding";
        const string global_encoding = $"global::{encoding}";
        const string code_writer = $"global::{CODE_WRITER}";

        return new TypeSource
        {
            Signature = "internal static partial class ContextExtensions",
            Trivia = new TriviaSource
            {
                Summary = "Extension methods for adding generated source code to a generator context.",
                Example = CONTEXT_EXTENSIONS_EXAMPLE
            },
            Methods = new[]
            {
                AddSource("GeneratorExecutionContext"),
                AddSourceEncoding("GeneratorExecutionContext"),
                AddSource("GeneratorPostInitializationContext"),
                AddSourceEncoding("GeneratorPostInitializationContext"),
                AddSource("IncrementalGeneratorPostInitializationContext"),
                AddSourceEncoding("IncrementalGeneratorPostInitializationContext"),
                AddSource("SourceProductionContext"),
                AddSourceEncoding("SourceProductionContext")
            }
        };

        static string[] Dependencies()
        {
            return [$"{CODE_WRITER}.ToString()", $"{CODE_WRITER}.Clear()"];
        }

        static MethodSource AddSource(string context)
        {
            return new MethodSource
            {
                Name = "AddSource",
                Signature = $"public static partial void AddSource(this {ms_analysis}.{context} context, string hintName, {code_writer} writer)",
                Implementation = (writer, in _) =>
                {
                    writer.AppendLine($"context.AddSource(hintName, {source_text}.From(writer.ToString(), {global_encoding}.UTF8));");
                    writer.AppendLine("writer.Clear();");
                },
                Dependencies = Dependencies(),
                Trivia = new TriviaSource
                {
                    Summary =
                        $"Adds source code from the provided {GetTypeTriviaReference(GLOBAL_CODE_WRITER, "CodeWriter", out _)} to the compilation with UTF-8 encoding.",
                    Remarks = "The writer will be cleared after adding the source.",
                    Parameters = new Dictionary<string, string>
                    {
                        { "context", "The context to add the source to." },
                        { "hintName", "An identifier that can be used to reference this source text, must be unique within this generator." },
                        { "writer", "The writer containing the source code to add." }
                    }
                }
            };
        }

        static MethodSource AddSourceEncoding(string context)
        {
            return new MethodSource
            {
                Name = "AddSource",
                Signature =
                    $"public static partial void AddSource(this {ms_analysis}.{context} context, string hintName, {code_writer} writer, {global_encoding} encoding)",
                Implementation = (writer, in _) =>
                {
                    writer.AppendLine($"context.AddSource(hintName, {source_text}.From(writer.ToString(), encoding));");
                    writer.AppendLine("writer.Clear();");
                },
                Dependencies = Dependencies(),
                Trivia = new TriviaSource
                {
                    Summary =
                        $"Adds source code from the provided {GetTypeTriviaReference(GLOBAL_CODE_WRITER, "CodeWriter", out _)} " +
                        $"to the compilation with a specified {GetTypeTriviaReference("global::System.Text.Encoding", "Encoding", out _)}.",
                    Remarks = "The writer will be cleared after adding the source.",
                    Parameters = new Dictionary<string, string>
                    {
                        { "context", "The context to add the source to." },
                        { "hintName", "An identifier that can be used to reference this source text, must be unique within this generator." },
                        { "writer", "The writer containing the source code to add." },
                        { "encoding", "Encoding of the file that will be saved." }
                    }
                }
            };
        }
    }
}