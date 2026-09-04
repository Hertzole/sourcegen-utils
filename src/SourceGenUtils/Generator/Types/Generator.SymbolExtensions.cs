using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private static TypeSource CreateSymbolExtensions()
    {
        TriviaSource hasAttributeTrivia = new TriviaSource
        {
            Summary = "Determines whether the given symbol has the specified attribute.",
            Returns = $"{TRIVIA_TRUE} if the symbol has the specified attribute; otherwise {TRIVIA_FALSE}.",
            Parameters = new Dictionary<string, string>
            {
                { "symbol", "The symbol to check." },
                {
                    "fullAttributeTypeName",
                    "The name of the attribute to check for. It needs to include the full namespace and type name. E.g <c>My.Namespace.MyAttribute</c>. It may also include the <c>global::</c> prefix."
                }
            }
        };

        TriviaSource tryGetAttributeTrivia = new TriviaSource
        {
            Summary = "Determines whether the given symbol has the specified attribute, and returns the attribute if it does.",
            Returns = hasAttributeTrivia.Returns,
            Parameters = new Dictionary<string, string>(hasAttributeTrivia.Parameters)
            {
                {
                    "attribute",
                    $"When this method returns, contains the attribute with the specified name, if the attribute was found; otherwise, {TRIVIA_NULL}. This parameter is passed uninitialized."
                }
            }
        };

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
                },
                new MethodSource
                {
                    Name = "HasAttribute",
                    Signature = $"public static partial bool HasAttribute(this {MS_CODE}.ISymbol symbol, string fullAttributeTypeName)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine($"return HasAttribute(symbol, {GLOBAL_MEMORY_EXT}.AsSpan(fullAttributeTypeName));");
                    },
                    Dependencies = [$"{SYMBOL_EXTENSIONS}.HasAttribute({MS_CODE}.ISymbol, {R_SPAN}<char>)"],
                    Trivia = hasAttributeTrivia,
                    EmptyStub = "return false;"
                },
                new MethodSource
                {
                    Name = "HasAttribute",
                    Signature = $"public static partial bool HasAttribute(this {GLOBAL_MS_CODE}.ISymbol symbol, {GLOBAL_R_SPAN}<char> fullAttributeTypeName)",
                    Implementation = (writer, in _) => { writer.AppendLine("return TryGetAttribute(symbol, fullAttributeTypeName, out _);"); },
                    EmptyStub = "return false;",
                    Dependencies = [$"{SYMBOL_EXTENSIONS}.TryGetAttribute({MS_CODE}.ISymbol, {R_SPAN}<char>, {MS_CODE}.AttributeData)"],
                    Trivia = hasAttributeTrivia
                },
                new MethodSource
                {
                    Name = "TryGetAttribute",
                    Signature =
                        $"public static partial bool TryGetAttribute(this {GLOBAL_MS_CODE}.ISymbol symbol, string fullAttributeTypeName, out {GLOBAL_MS_CODE}.AttributeData? attribute)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine($"return TryGetAttribute(symbol, {GLOBAL_MEMORY_EXT}.AsSpan(fullAttributeTypeName), out attribute);");
                    },
                    EmptyStub = "attribute = null; return false;",
                    Dependencies = [$"{SYMBOL_EXTENSIONS}.TryGetAttribute({MS_CODE}.ISymbol, {R_SPAN}<char>, {MS_CODE}.AttributeData)"],
                    Trivia = tryGetAttributeTrivia
                },
                new MethodSource
                {
                    Name = "TryGetAttribute",
                    Signature =
                        $"public static partial bool TryGetAttribute(this {GLOBAL_MS_CODE}.ISymbol symbol, {GLOBAL_R_SPAN}<char> fullAttributeTypeName, out {GLOBAL_MS_CODE}.AttributeData? attribute)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendIndentedSource(
                            $$"""
                              {{GLOBAL_IMMUTABLE}}.ImmutableArray<{{GLOBAL_MS_CODE}}.AttributeData> attributes = symbol.GetAttributes();
                              if (attributes.Length == 0)
                              {
                                  attribute = null;
                                  return false;
                              }

                              {{GLOBAL_SPAN}}<char> attributeName = stackalloc char[{{GLOBAL_VARIABLE_NAMES}}.GetNameWithGlobalPrefixLength(fullAttributeTypeName)];
                              {{GLOBAL_VARIABLE_NAMES}}.AppendGlobalPrefix(fullAttributeTypeName, attributeName);

                              for (int i = 0; i < attributes.Length; i++)
                              {
                                  {{GLOBAL_MS_CODE}}.INamedTypeSymbol? attributeClass = attributes[i].AttributeClass;
                                  if (attributeClass == null)
                                  {
                                      continue;
                                  }
                                  
                                  {{GLOBAL_R_SPAN}}<char> className = {{GLOBAL_MEMORY_EXT}}.AsSpan(attributeClass.ToDisplayString({{GLOBAL_MS_CODE}}.NullableFlowState.NotNull, {{GLOBAL_MS_CODE}}.SymbolDisplayFormat.FullyQualifiedFormat));
                                  if ({{GLOBAL_MEMORY_EXT}}.Equals(attributeName, className, global::System.StringComparison.Ordinal))
                                  {
                                      attribute = attributes[i];
                                      return true;
                                  }
                              }

                              attribute = null;
                              return false;
                              """);
                    },
                    EmptyStub = "attribute = null; return false;",
                    Dependencies =
                    [
                        $"{VARIABLE_NAMES}.GetNameWithGlobalPrefixLength({R_SPAN}<char>)",
                        $"{VARIABLE_NAMES}.AppendGlobalPrefix({R_SPAN}<char>, {SPAN}<char>)"
                    ],
                    Trivia = tryGetAttributeTrivia
                }
            ]
        };
    }
}