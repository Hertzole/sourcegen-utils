using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
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
                    Summary = $"Adds source code from the provided {GetTypeTriviaReference(GLOBAL_CODE_WRITER, "CodeWriter", out _)} to the compilation.",
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