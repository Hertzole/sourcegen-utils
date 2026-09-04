using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private static TypeSource CreateStringBuilderExtensions()
    {
        const string sb = "System.Text.StringBuilder";
        const string g_sb = $"global::{sb}";

        return new TypeSource
        {
            Signature = "internal static partial class StringBuilderExtensions",
            Methods =
            [
                new MethodSource
                {
                    Name = "Append",
                    Signature = $"public static partial {g_sb} Append(this {g_sb} builder, {GLOBAL_R_SPAN}<char> value)",
                    Implementation = (writer, in context) =>
                    {
                        if (context.AllowUnsafe)
                        {
                            writer.AppendIndentedSource("""
                                                        if (value.Length > 0)
                                                        {
                                                            unsafe
                                                            {
                                                                fixed (char* buffer = value)
                                                                {
                                                                    builder.Append(buffer, value.Length);
                                                                }
                                                            }
                                                        }
                                                        """);
                        }
                        else
                        {
                            writer.AppendIndentedSource("""
                                                        if (value.Length > 0)
                                                        {
                                                            // Consider allowing unsafe code in your project to use pointers here instead.
                                                            builder.EnsureCapacity(builder.Length + value.Length);
                                                            for (int i = 0; i < value.Length; i++)
                                                            {
                                                                builder.Append(value[i]);
                                                            }
                                                        }
                                                        """);
                        }

                        writer.AppendLine();
                        writer.AppendLine("return builder;");
                    },
                    EmptyStub = "return builder;",
                    Trivia = new TriviaSource
                    {
                        Summary = "Appends the string representation of a specified read-only character span to this instance.",
                        Returns = "A reference to this instance after the append operation is completed.",
                        Parameters = new Dictionary<string, string>
                        {
                            { "builder", "The StringBuilder instance to append to." },
                            { "value", "The read-only character span to append." }
                        }
                    }
                },
                new MethodSource
                {
                    Name = "AppendLine",
                    Signature = $"public static partial {g_sb} AppendLine(this {g_sb} builder, {GLOBAL_R_SPAN}<char> value)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendIndentedSource("""
                                                    Append(builder, value);
                                                    builder.AppendLine();
                                                    return builder;
                                                    """);
                    },
                    Dependencies = [STRING_BUILDER_EXTENSIONS + $".Append({sb}, {R_SPAN}<char>)"],
                    EmptyStub = "return builder;",
                    Trivia = new TriviaSource
                    {
                        Summary =
                            "Appends the string representation of a specified read-only character span followed by the default line terminator to the end of the current StringBuilder object",
                        Returns = "A reference to this instance after the append operation is completed.",
                        Parameters = new Dictionary<string, string>
                        {
                            { "builder", "The StringBuilder instance to append to." },
                            { "value", "The read-only character span to append." }
                        }
                    }
                }
            ]
        };
    }
}