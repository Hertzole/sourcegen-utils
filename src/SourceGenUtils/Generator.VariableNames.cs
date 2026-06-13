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
                    Signature = "public static partial global::System.ReadOnlySpan<char> NicifyVariableName(global::System.ReadOnlySpan<char> value)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("value = RemovePrefix(value);");
                        writer.AppendLine("value = UppercaseStart(value);");
                        writer.AppendLine("return value;");
                    },
                    EmptyStub = "return value;",
                    Dependencies = [variable_names + ".RemovePrefix(System.ReadOnlySpan<char>)", variable_names + ".UppercaseStart(System.ReadOnlySpan<char>)"]
                },
                new MethodSource
                {
                    Name = "RemovePrefix",
                    Signature = "public static partial global::System.ReadOnlySpan<char> RemovePrefix(global::System.ReadOnlySpan<char> value)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("// Check for prefixes like 'm_'.");
                        writer.AppendLine("if (value.Length > 2 && value[1] == '_')");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("return value.Slice(2);");
                        }

                        writer.AppendLine();
                        writer.AppendLine("// Check for names that start with '_' or 'k' (konstants).");
                        writer.AppendLine("if (value.Length > 1 && (value[0] == '_' || value[0] == 'k'))");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("return value.Slice(1);");
                        }

                        writer.AppendLine();
                        writer.AppendLine("return value;");
                    },
                    EmptyStub = "return value;"
                },
                new MethodSource
                {
                    Name = "UppercaseStart",
                    Signature = "public static partial global::System.ReadOnlySpan<char> UppercaseStart(global::System.ReadOnlySpan<char> value)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("if (value.Length == 0)");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("return value;");
                        }

                        writer.AppendLine();
                        writer.AppendLine("if (value[0] == char.ToUpperInvariant(value[0]))");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("// Already uppercase.");
                            writer.AppendLine("return value;");
                        }

                        writer.AppendLine();
                        writer.AppendLine("char[] newValue = global::System.Buffers.ArrayPool<char>.Shared.Rent(value.Length);");
                        writer.AppendLine("try");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("value.CopyTo(newValue);");
                            writer.AppendLine("newValue[0] = char.ToUpperInvariant(value[0]);");
                            writer.AppendLine("return new System.ReadOnlySpan<char>(newValue, 0, value.Length);");
                        }

                        writer.AppendLine("finally");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("global::System.Buffers.ArrayPool<char>.Shared.Return(newValue);");
                        }
                    },
                    EmptyStub = "return value;"
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