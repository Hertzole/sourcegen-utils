using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private static TypeSource CreateVariableNames()
    {
        const string variable_names = NAMESPACE + ".VariableNames";
        const string nicify_trivia = "Removes common prefixes (e.g. <c>m_</c>, <c>_</c>, <c>k</c>) and uppercases the first character.";

        TriviaSource getNiceNameLengthTrivia = new TriviaSource
        {
            Summary = "Calculates the required length needed for a buffer to support the new nice name.",
            Returns = "The length required for a nice name.",
            Parameters = new Dictionary<string, string>
            {
                ["value"] = "The string to get the length for."
            }
        };

        return new TypeSource
        {
            Signature = "internal static partial class VariableNames",
            Trivia = new TriviaSource
            {
                Summary = "Utilities for transforming variable names into a more readable form."
            },
            Methods =
            [
                new MethodSource
                {
                    Name = "NicifyVariableName",
                    Signature = "public static partial int NicifyVariableName(global::System.ReadOnlySpan<char> value, global::System.Span<char> destination)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("int written = RemovePrefix(value, destination);");
                        writer.AppendLine("UppercaseStart(destination.Slice(0, written), destination);");
                        writer.AppendLine("return written;");
                    },
                    EmptyStub = "return 0;",
                    Dependencies =
                    [
                        variable_names + ".RemovePrefix(System.ReadOnlySpan<char>, System.Span<char>)",
                        variable_names + ".UppercaseStart(System.ReadOnlySpan<char>, System.Span<char>)"
                    ],
                    Trivia = new TriviaSource
                    {
                        Summary = nicify_trivia,
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The variable name to transform.",
                            ["destination"] = "The buffer to write the result to."
                        },
                        Returns = "The number of characters written to the destination."
                    }
                },
                new MethodSource
                {
                    Name = "NicifyVariableName",
                    Signature = $"public static partial int NicifyVariableName(global::System.ReadOnlySpan<char> value, global::{ARRAY_BUILDER}<char> builder)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(
                            "char[] destination = global::System.Buffers.ArrayPool<char>.Shared.Rent(GetNiceNameLength(value));");

                        writer.AppendLine(
                            "int written = NicifyVariableName(value, global::System.MemoryExtensions.AsSpan(destination));");

                        writer.AppendLine("builder.AddRange(global::System.MemoryExtensions.AsSpan(destination, 0, written));");
                        writer.AppendLine("global::System.Buffers.ArrayPool<char>.Shared.Return(destination);");
                        writer.AppendLine("return written;");
                    },
                    EmptyStub = "return 0;",
                    Dependencies =
                    [
                        variable_names + ".NicifyVariableName(System.ReadOnlySpan<char>, System.Span<char>)",
                        variable_names + ".GetNiceNameLength(System.ReadOnlySpan<char>)",
                        ARRAY_BUILDER + ".AddRange(System.ReadOnlySpan<T>)"
                    ],
                    Trivia = new TriviaSource
                    {
                        Summary = nicify_trivia,
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The variable name to transform.",
                            ["destination"] = "The buffer to write the result to."
                        },
                        Returns = "The number of characters written to the destination."
                    }
                },
                new MethodSource
                {
                    Name = "NicifyVariableName",
                    Signature = "public static partial string NicifyVariableName(string value)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(
                            "char[] destination = global::System.Buffers.ArrayPool<char>.Shared.Rent(GetNiceNameLength(global::System.MemoryExtensions.AsSpan(value)));");

                        writer.AppendLine(
                            "int written = NicifyVariableName(global::System.MemoryExtensions.AsSpan(value), global::System.MemoryExtensions.AsSpan(destination));");

                        writer.AppendLine("string result = global::System.MemoryExtensions.AsSpan(destination, 0, written).ToString();");
                        writer.AppendLine("global::System.Buffers.ArrayPool<char>.Shared.Return(destination);");
                        writer.AppendLine("return result;");
                    },
                    EmptyStub = "return value;",
                    Dependencies =
                    [
                        variable_names + ".NicifyVariableName(System.ReadOnlySpan<char>, System.Span<char>)",
                        variable_names + ".GetNiceNameLength(System.ReadOnlySpan<char>)"
                    ],
                    Trivia = new TriviaSource
                    {
                        Summary = nicify_trivia,
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The variable name to transform."
                        },
                        Returns = "The nicified variable name."
                    }
                },
                new MethodSource
                {
                    Name = "NicifyVariableName",
                    Signature = $"public static partial int NicifyVariableName(string value, global::{ARRAY_BUILDER}<char> builder)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("return NicifyVariableName(global::System.MemoryExtensions.AsSpan(value), builder);");
                    },
                    EmptyStub = "return 0;",
                    Dependencies =
                    [
                        variable_names + $".NicifyVariableName(System.ReadOnlySpan<char>, {ARRAY_BUILDER}<char>)"
                    ],
                    Trivia = new TriviaSource
                    {
                        Summary = nicify_trivia,
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The variable name to transform."
                        },
                        Returns = "The nicified variable name."
                    }
                },
                new MethodSource
                {
                    Name = "RemovePrefix",
                    Signature = "public static partial int RemovePrefix(global::System.ReadOnlySpan<char> value, global::System.Span<char> destination)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("// Check for prefixes like 'm_'.");
                        writer.AppendLine("if (HasMemberPrefix(value))");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("value.Slice(2).CopyTo(destination);");
                            writer.AppendLine("return value.Length - 2;");
                        }

                        writer.AppendLine("// Check for names that start with '_' or 'k' (konstants).");
                        writer.AppendLine("if (value.Length > 1 && (value[0] == '_' || HasConstantPrefix(value)))");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("value.Slice(1).CopyTo(destination);");
                            writer.AppendLine("return value.Length - 1;");
                        }

                        writer.AppendLine("value.CopyTo(destination);");
                        writer.AppendLine("return value.Length;");
                    },
                    Dependencies =
                    [
                        variable_names + ".HasConstantPrefix(System.ReadOnlySpan<char>)",
                        variable_names + ".HasMemberPrefix(System.ReadOnlySpan<char>)"
                    ],
                    EmptyStub = "return 0;",
                    Trivia = new TriviaSource
                    {
                        Summary = "Removes common variable name prefixes such as <c>m_</c>, <c>_</c>, and <c>k</c>.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The variable name to process.",
                            ["destination"] = "The buffer to write the result to."
                        },
                        Returns = "The number of characters written to the destination."
                    }
                },
                new MethodSource
                {
                    Name = "RemovePrefix",
                    Signature = $"public static partial int RemovePrefix(global::System.ReadOnlySpan<char> value, global::{ARRAY_BUILDER}<char> builder)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("char[] destination = global::System.Buffers.ArrayPool<char>.Shared.Rent(GetNiceNameLength(value));");
                        writer.AppendLine("int written = RemovePrefix(value, global::System.MemoryExtensions.AsSpan(destination));");
                        writer.AppendLine("builder.AddRange(global::System.MemoryExtensions.AsSpan(destination, 0, written));");
                        writer.AppendLine("global::System.Buffers.ArrayPool<char>.Shared.Return(destination);");
                        writer.AppendLine("return written;");
                    },
                    EmptyStub = "return 0;",
                    Dependencies =
                    [
                        variable_names + ".RemovePrefix(System.ReadOnlySpan<char>, System.Span<char>)",
                        variable_names + ".GetNiceNameLength(System.ReadOnlySpan<char>)",
                        ARRAY_BUILDER + ".AddRange(System.ReadOnlySpan<T>)"
                    ],
                    Trivia = new TriviaSource
                    {
                        Summary = "Removes common variable name prefixes such as <c>m_</c>, <c>_</c>, and <c>k</c>.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The variable name to process."
                        },
                        Returns = "The variable name with the prefix removed."
                    }
                },
                new MethodSource
                {
                    Name = "RemovePrefix",
                    Signature = "public static partial string RemovePrefix(string value)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(
                            "char[] destination = global::System.Buffers.ArrayPool<char>.Shared.Rent(GetNiceNameLength(global::System.MemoryExtensions.AsSpan(value)));");

                        writer.AppendLine(
                            "int written = RemovePrefix(global::System.MemoryExtensions.AsSpan(value), global::System.MemoryExtensions.AsSpan(destination));");

                        writer.AppendLine("string result = global::System.MemoryExtensions.AsSpan(destination, 0, written).ToString();");
                        writer.AppendLine("global::System.Buffers.ArrayPool<char>.Shared.Return(destination);");
                        writer.AppendLine("return result;");
                    },
                    EmptyStub = "return string.Empty;",
                    Dependencies =
                    [
                        variable_names + ".RemovePrefix(System.ReadOnlySpan<char>, System.Span<char>)",
                        variable_names + ".GetNiceNameLength(System.ReadOnlySpan<char>)"
                    ],
                    Trivia = new TriviaSource
                    {
                        Summary = "Removes common variable name prefixes such as <c>m_</c>, <c>_</c>, and <c>k</c>.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The variable name to process."
                        },
                        Returns = "The variable name with the prefix removed."
                    }
                },
                new MethodSource
                {
                    Name = "RemovePrefix",
                    Signature = $"public static partial int RemovePrefix(string value, global::{ARRAY_BUILDER}<char> builder)",
                    Implementation = (writer, in _) => { writer.AppendLine("return RemovePrefix(global::System.MemoryExtensions.AsSpan(value), builder);"); },
                    EmptyStub = "return 0;",
                    Dependencies =
                    [
                        variable_names + $".RemovePrefix(System.ReadOnlySpan<char>, {ARRAY_BUILDER}<char>)"
                    ],
                    Trivia = new TriviaSource
                    {
                        Summary = "Removes common variable name prefixes such as <c>m_</c>, <c>_</c>, and <c>k</c>.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The variable name to process."
                        },
                        Returns = "The variable name with the prefix removed."
                    }
                },
                new MethodSource
                {
                    Name = "UppercaseStart",
                    Signature = "public static partial void UppercaseStart(global::System.ReadOnlySpan<char> value, global::System.Span<char> destination)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("if (value.Length == 0)");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("// Empty string.");
                            writer.AppendLine("value.CopyTo(destination);");
                            writer.AppendLine("return;");
                        }

                        writer.AppendLine("if (value[0] == char.ToUpperInvariant(value[0]))");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("// Already uppercase.");
                            writer.AppendLine("value.CopyTo(destination);");
                            writer.AppendLine("return;");
                        }

                        writer.AppendLine("value.CopyTo(destination);");
                        writer.AppendLine("destination[0] = char.ToUpperInvariant(value[0]);");
                    },
                    Trivia = new TriviaSource
                    {
                        Summary = "Uppercases the first character of the value and writes the result to the destination.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The input value.",
                            ["destination"] = "The buffer to write the result to."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "UppercaseStart",
                    Signature = $"public static partial void UppercaseStart(global::System.ReadOnlySpan<char> value, global::{ARRAY_BUILDER}<char> builder)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("if (value.Length == 0)");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("// Empty string.");
                            writer.AppendLine("return;");
                        }

                        writer.AppendLine("if (value[0] == char.ToUpperInvariant(value[0]))");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("// Already uppercase.");
                            writer.AppendLine("builder.AddRange(value);");
                            writer.AppendLine("return;");
                        }

                        writer.AppendLine("char[] destination = global::System.Buffers.ArrayPool<char>.Shared.Rent(value.Length);");
                        writer.AppendLine("value.CopyTo(global::System.MemoryExtensions.AsSpan(destination));");
                        writer.AppendLine("destination[0] = char.ToUpperInvariant(value[0]);");
                        writer.AppendLine("builder.AddRange(global::System.MemoryExtensions.AsSpan(destination, 0, value.Length));");
                        writer.AppendLine("global::System.Buffers.ArrayPool<char>.Shared.Return(destination);");
                    },
                    Dependencies =
                    [
                        ARRAY_BUILDER + ".AddRange(System.ReadOnlySpan<T>)"
                    ],
                    Trivia = new TriviaSource
                    {
                        Summary = "Returns a new string with the first character uppercased.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The input string."
                        },
                        Returns = "A new string with the first character uppercased."
                    }
                },
                new MethodSource
                {
                    Name = "UppercaseStart",
                    Signature = "public static partial string UppercaseStart(string value)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("if (value.Length == 0)");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("// Empty string.");
                            writer.AppendLine("return string.Empty;");
                        }

                        writer.AppendLine("if (value[0] == char.ToUpperInvariant(value[0]))");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("// Already uppercase.");
                            writer.AppendLine("return value;");
                        }

                        writer.AppendLine("char[] destination = global::System.Buffers.ArrayPool<char>.Shared.Rent(value.Length);");
                        writer.AppendLine("global::System.MemoryExtensions.AsSpan(value).CopyTo(global::System.MemoryExtensions.AsSpan(destination));");
                        writer.AppendLine("destination[0] = char.ToUpperInvariant(value[0]);");
                        writer.AppendLine("string result = global::System.MemoryExtensions.AsSpan(destination, 0, value.Length).ToString();");
                        writer.AppendLine("global::System.Buffers.ArrayPool<char>.Shared.Return(destination);");
                        writer.AppendLine("return result;");
                    },
                    EmptyStub = "return string.Empty;",
                    Trivia = new TriviaSource
                    {
                        Summary = "Returns a new string with the first character uppercased.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The input string."
                        },
                        Returns = "A new string with the first character uppercased."
                    }
                },
                new MethodSource
                {
                    Name = "UppercaseStart",
                    Signature = $"public static partial void UppercaseStart(string value, global::{ARRAY_BUILDER}<char> builder)",
                    Implementation = (writer, in _) => { writer.AppendLine("UppercaseStart(global::System.MemoryExtensions.AsSpan(value), builder);"); },
                    Dependencies =
                    [
                        variable_names + $".UppercaseStart(System.ReadOnlySpan<char>, {ARRAY_BUILDER}<char>)"
                    ],
                    Trivia = new TriviaSource
                    {
                        Summary = "Returns a new string with the first character uppercased.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The input string."
                        },
                        Returns = "A new string with the first character uppercased."
                    }
                },
                new MethodSource
                {
                    Name = "StartsWithOn",
                    Signature = "public static partial bool StartsWithOn(global::System.ReadOnlySpan<char> value)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("// Check if the value starts with 'on' or 'On' and that the third character is uppercase.");
                        writer.AppendLine("// Checking the third character ensures it doesn't match words like \"only\" or \"once\".");
                        writer.AppendLine("return value.Length >= 3 && (value[0] == 'o' || value[0] == 'O') && value[1] == 'n' && char.IsUpper(value[2]);");
                    },
                    EmptyStub = "return false;",
                    Trivia = new TriviaSource
                    {
                        Summary =
                            "Determines whether the value starts with <c>on</c> or <c>On</c> followed by an uppercase character (e.g. <c>OnValueChanged</c>).",
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The value to check."
                        },
                        Returns = "<c>true</c> if the value starts with <c>on</c> or <c>On</c> followed by an uppercase character; otherwise <c>false</c>."
                    }
                },
                new MethodSource
                {
                    Name = "StartsWithOn",
                    Signature = "public static partial bool StartsWithOn(string? value)",
                    Implementation = (writer, in _) => { writer.AppendLine("return StartsWithOn(global::System.MemoryExtensions.AsSpan(value));"); },
                    Dependencies =
                    [
                        variable_names + ".StartsWithOn(System.ReadOnlySpan<char>)"
                    ],
                    EmptyStub = "return false;",
                    Trivia = new TriviaSource
                    {
                        Summary =
                            "Determines whether the value starts with <c>on</c> or <c>On</c> followed by an uppercase character (e.g. <c>OnValueChanged</c>).",
                        Parameters = new Dictionary<string, string>
                        {
                            ["value"] = "The value to check."
                        },
                        Returns = "<c>true</c> if the value starts with <c>on</c> or <c>On</c> followed by an uppercase character; otherwise <c>false</c>."
                    }
                },
                new MethodSource
                {
                    Name = "GetNiceNameLength",
                    Signature = "public static partial int GetNiceNameLength(string value)",
                    Implementation = (writer, in _) => { writer.AppendLine("return GetNiceNameLength(global::System.MemoryExtensions.AsSpan(value));"); },
                    Dependencies = [variable_names + ".GetNiceNameLength(System.ReadOnlySpan<char>)"],
                    EmptyStub = "return 0;",
                    Trivia = getNiceNameLengthTrivia
                },
                new MethodSource
                {
                    Name = "GetNiceNameLength",
                    Signature = "public static partial int GetNiceNameLength(global::System.ReadOnlySpan<char> value)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("if (value.Length == 0)");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("return 0;");
                        }

                        writer.AppendLine("// Remove '_' prefix or k/K (konstant) prefix.");
                        writer.AppendLine("if (value.Length > 1 && value[0] == '_' || HasConstantPrefix(value))");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("return value.Length - 1;");
                        }

                        writer.AppendLine("// Remove prefixes like 'm_'.");
                        writer.AppendLine("if (HasMemberPrefix(value))");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("return value.Length - 2;");
                        }

                        writer.AppendLine("return value.Length;");
                    },
                    EmptyStub = "return 0;",
                    Dependencies =
                    [
                        variable_names + ".HasConstantPrefix(System.ReadOnlySpan<char>)",
                        variable_names + ".HasMemberPrefix(System.ReadOnlySpan<char>)"
                    ],
                    Trivia = getNiceNameLengthTrivia
                },
                new MethodSource
                {
                    Name = "HasConstantPrefix",
                    Signature = "private static bool HasConstantPrefix(global::System.ReadOnlySpan<char> value)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("if (value.Length <= 1)");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("return false;");
                        }

                        writer.AppendLine("return (value[0] == 'k' || value[0] == 'K') && char.IsUpper(value[1]);");
                    },
                    SkipPartial = true,
                    EmptyStub = "return false;"
                },
                new MethodSource
                {
                    Name = "HasMemberPrefix",
                    Signature = "private static bool HasMemberPrefix(global::System.ReadOnlySpan<char> value)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("if (value.Length <= 2)");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("return false;");
                        }

                        writer.AppendLine("return value.Length > 2 && value[1] == '_';");
                    },
                    SkipPartial = true,
                    EmptyStub = "return false;"
                }
            ]
        };
    }
}