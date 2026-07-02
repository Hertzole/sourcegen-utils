namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private static TypeSource CreateVariableNames()
    {
        const string variable_names = NAMESPACE + ".VariableNames";

        return new TypeSource
        {
            Signature = "internal static partial class VariableNames",
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
                    ]
                },
                new MethodSource
                {
                    Name = "NicifyVariableName",
                    Signature = "public static partial string NicifyVariableName(string value)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("char[] destination = global::System.Buffers.ArrayPool<char>.Shared.Rent(value.Length);");
                        writer.AppendLine(
                            "int written = NicifyVariableName(global::System.MemoryExtensions.AsSpan(value), global::System.MemoryExtensions.AsSpan(destination));");

                        writer.AppendLine("string result = global::System.MemoryExtensions.AsSpan(destination, 0, written).ToString();");
                        writer.AppendLine("global::System.Buffers.ArrayPool<char>.Shared.Return(destination);");
                        writer.AppendLine("return result;");
                    },
                    EmptyStub = "return value;",
                    Dependencies = [variable_names + ".NicifyVariableName(System.ReadOnlySpan<char>, System.Span<char>)"]
                },
                new MethodSource
                {
                    Name = "RemovePrefix",
                    Signature = "public static partial int RemovePrefix(global::System.ReadOnlySpan<char> value, global::System.Span<char> destination)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("// Check for prefixes like 'm_'.");
                        writer.AppendLine("if (value.Length > 2 && value[1] == '_')");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("value.Slice(2).CopyTo(destination);");
                            writer.AppendLine("return value.Length - 2;");
                        }

                        writer.AppendLine();
                        writer.AppendLine("// Check for names that start with '_' or 'k' (konstants).");
                        writer.AppendLine("if (value.Length > 1 && (value[0] == '_' || value[0] == 'k'))");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("value.Slice(1).CopyTo(destination);");
                            writer.AppendLine("return value.Length - 1;");
                        }

                        writer.AppendLine();
                        writer.AppendLine("value.CopyTo(destination);");
                        writer.AppendLine("return value.Length;");
                    },
                    EmptyStub = "return 0;"
                },
                new MethodSource
                {
                    Name = "RemovePrefix",
                    Signature = "public static partial string RemovePrefix(string value)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("char[] destination = global::System.Buffers.ArrayPool<char>.Shared.Rent(value.Length);");
                        writer.AppendLine(
                            "int written = RemovePrefix(global::System.MemoryExtensions.AsSpan(value), global::System.MemoryExtensions.AsSpan(destination));");

                        writer.AppendLine("string result = global::System.MemoryExtensions.AsSpan(destination, 0, written).ToString();");
                        writer.AppendLine("global::System.Buffers.ArrayPool<char>.Shared.Return(destination);");
                        writer.AppendLine("return result;");
                    },
                    EmptyStub = "return string.Empty;",
                    Dependencies = [variable_names + ".RemovePrefix(System.ReadOnlySpan<char>, System.Span<char>)"]
                },
                new MethodSource
                {
                    Name = "UppercaseStart",
                    Signature = "public static partial void UppercaseStart(global::System.ReadOnlySpan<char> value, global::System.Span<char> destination)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("if (value.Length == 0)");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("// Empty string.");
                            writer.AppendLine("value.CopyTo(destination);");
                            writer.AppendLine("return;");
                        }

                        writer.AppendLine();
                        writer.AppendLine("if (value[0] == char.ToUpperInvariant(value[0]))");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("// Already uppercase.");
                            writer.AppendLine("value.CopyTo(destination);");
                            writer.AppendLine("return;");
                        }

                        writer.AppendLine();
                        writer.AppendLine("value.CopyTo(destination);");
                        writer.AppendLine("destination[0] = char.ToUpperInvariant(value[0]);");
                    },
                },
                new MethodSource
                {
                    Name = "UppercaseStart",
                    Signature = "public static partial string UppercaseStart(string value)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("if (value.Length == 0)");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("// Empty string.");
                            writer.AppendLine("return string.Empty;");
                        }

                        writer.AppendLine();
                        writer.AppendLine("if (value[0] == char.ToUpperInvariant(value[0]))");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("// Already uppercase.");
                            writer.AppendLine("return value;");
                        }

                        writer.AppendLine();
                        writer.AppendLine("char[] destination = global::System.Buffers.ArrayPool<char>.Shared.Rent(value.Length);");
                        writer.AppendLine("global::System.MemoryExtensions.AsSpan(value).CopyTo(global::System.MemoryExtensions.AsSpan(destination));");
                        writer.AppendLine("destination[0] = char.ToUpperInvariant(value[0]);");
                        writer.AppendLine("string result = global::System.MemoryExtensions.AsSpan(destination, 0, value.Length).ToString();");
                        writer.AppendLine("global::System.Buffers.ArrayPool<char>.Shared.Return(destination);");
                        writer.AppendLine("return result;");
                    },
                    EmptyStub = "return string.Empty;"
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
                    EmptyStub = "return false;"
                }
            ]
        };
    }
}