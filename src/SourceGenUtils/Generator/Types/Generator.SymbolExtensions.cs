namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private static TypeSource CreateSymbolExtensions()
    {
        return new TypeSource
        {
            Signature = "internal static partial class SymbolExtensions",
            Methods =
            [
                new MethodSource
                {
                    Name = "GetDeclarationString",
                    Signature = "public static partial string GetDeclarationString(this global::Microsoft.CodeAnalysis.ITypeSymbol symbol, bool isPartial)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("if (symbol.IsReferenceType)");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("if (isPartial)");
                            using (writer.WithBlock(true))
                            {
                                writer.AppendLine("if (symbol.IsRecord)");
                                using (writer.WithBlock(true))
                                {
                                    writer.AppendLine("return \"partial record\";");
                                }

                                writer.AppendLine("return symbol.IsStatic ? \"static partial class\" : \"partial class\";");
                            }

                            // Not partial
                            writer.AppendLine("if (symbol.IsRecord)");
                            using (writer.WithBlock(true))
                            {
                                writer.AppendLine("return \"record\";");
                            }

                            // Not record
                            writer.AppendLine("return symbol.IsStatic ? \"static class\" : \"class\";");
                        }

                        // Must be value type.
                        writer.AppendLine("if (symbol.IsRecord)");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("if (isPartial)");
                            using (writer.WithBlock(true))
                            {
                                writer.AppendLine("return symbol.IsReadOnly ? \"readonly partial record struct\" : \"partial record struct\";");
                            }

                            writer.AppendLine("return symbol.IsReadOnly ? \"readonly record struct\" : \"record struct\";");
                        }

                        // Not a record.
                        writer.AppendLine("if (isPartial)");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("return symbol.IsReadOnly ? \"readonly partial struct\" : \"partial struct\";");
                        }

                        // Not partial, just your average struct.
                        writer.AppendLine("return symbol.IsReadOnly ? \"readonly struct\" : \"struct\";");
                    },
                    EmptyStub = "return string.Empty;"
                }
            ]
        };
    }
}