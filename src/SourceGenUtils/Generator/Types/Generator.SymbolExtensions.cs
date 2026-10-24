using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private const string SYMBOL_EXTENSIONS_EXAMPLE =
        """
        The following example checks a symbol for an attribute and gets its declaration string.

        <code>
        using System;
        using Hertzole.SourceGen;
        using Microsoft.CodeAnalysis;

        public static class SymbolInspector
        {
            public static void Inspect(INamedTypeSymbol symbol)
            {
                // Checks for the fully qualified attribute name.
                if (symbol.HasAttribute("My.Namespace.MyAttribute"))
                {
                    Console.WriteLine("The type has the attribute.");
                }

                // Gets the attribute data if the attribute exists.
                if (symbol.TryGetAttribute("My.Namespace.MyAttribute", out AttributeData? attribute))
                {
                    Console.WriteLine(attribute);
                }

                // Returns e.g. "partial class" for a partial class symbol.
                string declaration = symbol.GetDeclarationString(isPartial: true);
                Console.WriteLine(declaration);
            }
        }
        </code>
        """;

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
            Trivia = new TriviaSource
            {
                Summary = "Extension methods for working with symbols.",
                Example = SYMBOL_EXTENSIONS_EXAMPLE
            },
            Methods =
            [
                new MethodSource
                {
                    Name = "GetDeclarationString",
                    Signature = "public static partial string GetDeclarationString(this global::Microsoft.CodeAnalysis.ITypeSymbol symbol, bool isPartial)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendIndentedSource(
                            $$"""
                              if (symbol.IsReferenceType)
                              {
                                  if (isPartial)
                                  {
                                      if (symbol.TypeKind == {{GLOBAL_MS_CODE}}.TypeKind.Interface)
                                      {
                                          return "partial interface";
                                      }
                                      else if (symbol.IsRecord)
                                      {
                                          return "partial record";
                                      }

                                      return symbol.IsStatic ? "static partial class" : "partial class";
                                  }

                                  if (symbol.TypeKind == {{GLOBAL_MS_CODE}}.TypeKind.Interface)
                                  {
                                      return "interface";
                                  }
                                  else if (symbol.IsRecord)
                                  {
                                      return "record";
                                  }

                                  return symbol.IsStatic ? "static class" : "class";
                              }

                              if (symbol.IsRecord)
                              {
                                  if (isPartial)
                                  {
                                      return symbol.IsReadOnly ? "readonly partial record struct" : "partial record struct";
                                  }

                                  return symbol.IsReadOnly ? "readonly record struct" : "record struct";
                              }

                              if (isPartial)
                              {
                                  return symbol.IsReadOnly ? "readonly partial struct" : "partial struct";
                              }

                              return symbol.IsReadOnly ? "readonly struct" : "struct";
                              """);
                    },
                    EmptyStub = "return string.Empty;",
                    Trivia = new TriviaSource
                    {
                        Summary = "Gets the type declaration, without access modifiers, for the provided symbol.<br/>\n" +
                                  $"If <paramref name=\"isPartial\"/> is {TRIVIA_TRUE} then the type declaration will include the <c>partial</c> keyword.",
                        Returns = "The type declaration.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["symbol"] = "The symbol to get the declaration from.",
                            ["isPartial"] = "Whether the type declaration should include the <c>partial</c> keyword."
                        },
                        Example =
                            $$"""
                              The following example shows how to get the declaration string of a symbol.

                              <code>
                              using Hertzole.SourceGen;
                              using {{MS_CODE}};

                              internal static class CodeHelper
                              {
                                  public static void AppendDeclaration(CodeWriter writer, ITypeSymbol symbol, bool isPartial)
                                  {
                                      writer.Append(symbol.GetDeclarationString(isPartial));
                                      
                                      // If the symbol is...
                                      //   an interface -> "interface"
                                      //   a class -> "class"
                                      //   a static class -> "static class"
                                      //   a record -> "record"
                                      //   a struct -> "struct"
                                      //   a readonly struct -> "readonly struct"
                                      //   a record struct -> "record struct"
                                      //   a readonly record struct -> "readonly record struct"
                                      // If partial is true, then the declaration will include the "partial" keyword.
                                  }
                              }
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