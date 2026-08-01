using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private static TypeSource CreateSymbolExtensions()
    {
        return new TypeSource
        {
            Signature = "internal static partial class SymbolExtensions",
            Trivia = "Extension methods for working with symbols.",
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
                    EmptyStub = "return string.Empty;",
                    Trivia = new TriviaSource
                    {
                        Summary = "Gets the type declaration, without access modifiers, for the provided symbol.<br/>\n" +
                                  "If <paramref name=\"isPartial\"/> is <see langword=\"true\"/> then the type declaration will include the <c>partial</c> keyword.",
                        Returns = "The type declaration.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["symbol"] = "The symbol to get the declaration from.",
                            ["isPartial"] = "Whether the type declaration should include the <c>partial</c> keyword."
                        },
                        Example = """
                                  <code>
                                  ITypeSymbol classSymbol = ... public class MyClass {}
                                  classSymbol.GetDeclarationString(false); // class MyClass
                                  classSymbol.GetDeclarationString(true);  // partial class MyClass
                                  </code>

                                  <code>
                                  ITypeSymbol structSymbol = ... public struct MyStruct [}
                                  structSymbol.GetDeclarationString(false); // struct MyStruct
                                  </code>

                                  <code>
                                  ITypeSymbol readonlyStructSymbol = ... private readonly struct MyReadonlyStruct {}
                                  readonlyStructSymbol.GetDeclarationString(false); // readonly struct MyReadonlyStruct
                                  readonlyStructSymbol.GetDeclarationString(true);  // partial readonly struct MyReadonlyStruct
                                  </code>
                                  """
                    }
                }
            ]
        };
    }
}