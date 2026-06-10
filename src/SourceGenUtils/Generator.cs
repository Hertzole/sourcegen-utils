using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Hertzole.SourceGenUtils;

[Generator]
public class Generator : IIncrementalGenerator
{
    private static readonly Dictionary<string, TypeSource> TypesToGenerate = new Dictionary<string, TypeSource>
    {
        ["CodeWriter"] = new TypeSource
        {
            Signature = "internal sealed class CodeWriter",
            Fields = new Dictionary<string, FieldSource>
            {
                ["_items"] = new FieldSource
                {
                    Signature = "private System.Collections.Generic.List<object> _items;",
                    Dependencies = new[] { "CodeWriter.Test" }
                }
            },
            Properties = new Dictionary<string, PropertySource>
            {
                ["Items"] = new PropertySource
                {
                    Signature = "public System.Collections.Generic.List<object> Items { get; set; }"
                    // Dependencies = new[] { "CodeWriter.Test" }
                }
            },
            Methods = new Dictionary<string, MethodSource>
            {
                ["Test"] = new MethodSource
                {
                    Signature = "public void Test()",
                    ReturnStub = string.Empty,
                    Implementation = "throw new System.NotImplementedException();",
                    Dependencies = new[] { "ArrayBuilder.Add" }
                },
                ["StaticTest"] = new MethodSource
                {
                    Signature = "public static void StaticTest()",
                    ReturnStub = string.Empty,
                    Implementation = "throw new System.NotImplementedException();",
                    Dependencies = new[] { "ArrayBuilder.Add" }
                }
            }
        },
        ["ArrayBuilder"] = new TypeSource
        {
            Signature = "internal sealed class ArrayBuilder",
            Methods = new Dictionary<string, MethodSource>
            {
                ["Add"] = new MethodSource
                {
                    Signature = "public void Add<T>(T item)",
                    ReturnStub = string.Empty,
                    Implementation = "throw new System.NotImplementedException();"
                }
            }
        }
    };

    private static readonly HashSet<string> AllClassNames;
    private static readonly HashSet<string> AllMethodNames;
    private static readonly Dictionary<string, string> MethodToUniqueType;

    static Generator()
    {
        AllClassNames = new HashSet<string>(TypesToGenerate.Keys);
        AllMethodNames = new HashSet<string>();
        MethodToUniqueType = new Dictionary<string, string>();
        Dictionary<string, int> methodCounts = new Dictionary<string, int>();

        foreach (KeyValuePair<string, TypeSource> typeKvp in TypesToGenerate)
        {
            foreach (string methodName in typeKvp.Value.Methods.Keys)
            {
                AllMethodNames.Add(methodName);
                methodCounts.TryGetValue(methodName, out int count);
                methodCounts[methodName] = count + 1;
                MethodToUniqueType[methodName] = typeKvp.Key;
            }
        }

        foreach (KeyValuePair<string, int> kvp in methodCounts)
        {
            if (kvp.Value != 1)
            {
                MethodToUniqueType.Remove(kvp.Key);
            }
        }
    }

    private const string Namespace = "Hertzole.SourceGen";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<string?>> calledMethods =
            context.SyntaxProvider
                   .CreateSyntaxProvider(
                       (s, _) => s is InvocationExpressionSyntax invocation
                                 && invocation.Expression is MemberAccessExpressionSyntax maes
                                 && maes.Name is IdentifierNameSyntax name
                                 && AllMethodNames.Contains(name.Identifier.Text),
                       (ctx, _) =>
                       {
                           InvocationExpressionSyntax invocation = (InvocationExpressionSyntax) ctx.Node;
                           MemberAccessExpressionSyntax maes = (MemberAccessExpressionSyntax) invocation.Expression;
                           string methodName = maes.Name.Identifier.Text;

                           // Direct type.Method() call — syntactic, works even on first build
                           if (maes.Expression is IdentifierNameSyntax id
                               && AllClassNames.Contains(id.Identifier.Text)
                               && TypesToGenerate.TryGetValue(id.Identifier.Text, out TypeSource? type)
                               && type.ContainsMethod(methodName))
                           {
                               return $"{id.Identifier.Text}.{methodName}";
                           }

                           // Instance call — use semantic model to check the containing type
                           IMethodSymbol? methodSymbol =
                               ctx.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

                           if (methodSymbol?.ContainingType != null)
                           {
                               string containingType = methodSymbol.ContainingType.ToDisplayString();
                               foreach (KeyValuePair<string, TypeSource> kvp in TypesToGenerate)
                               {
                                   if (containingType == $"{Namespace}.{kvp.Key}"
                                       && kvp.Value.ContainsMethod(methodName))
                                   {
                                       return $"{kvp.Key}.{methodName}";
                                   }
                               }
                           }

                           // Fallback: if the method name is unique to one generated type,
                           // assume this invocation targets it (handles first-build instance calls).
                           if (MethodToUniqueType.TryGetValue(methodName, out string? uniqueType))
                           {
                               return $"{uniqueType}.{methodName}";
                           }

                           return null;
                       })
                   .Where(name => name != null)
                   .Collect();

        context.RegisterSourceOutput(calledMethods,
            (ctx, t) =>
            {
                HashSet<string> calledSet = new HashSet<string>(t.Distinct()!);
                calledSet = ExpandDependencies(calledSet);
                GenerateCode(ctx, calledSet);
            });
    }

    private static bool MemberDependenciesMet(string[]? dependencies, HashSet<string> calledMethods)
    {
        if (dependencies == null || dependencies.Length == 0)
        {
            return true;
        }

        foreach (string dep in dependencies)
        {
            if (!calledMethods.Contains(dep))
            {
                return false;
            }
        }

        return true;
    }

    private static void GenerateCode(SourceProductionContext context, HashSet<string> calledMethods)
    {
        foreach (KeyValuePair<string, TypeSource> kvp in TypesToGenerate)
        {
            string className = kvp.Key;
            Dictionary<string, MethodSource> methods = kvp.Value.Methods;

            StringBuilder body = new StringBuilder();

            if (kvp.Value.Fields != null)
            {
                foreach (KeyValuePair<string, FieldSource> fieldKvp in kvp.Value.Fields)
                {
                    if (MemberDependenciesMet(fieldKvp.Value.Dependencies, calledMethods))
                    {
                        body.AppendLine($"        {fieldKvp.Value.Signature}");
                    }
                }
            }

            if (kvp.Value.Properties != null)
            {
                foreach (KeyValuePair<string, PropertySource> propKvp in kvp.Value.Properties)
                {
                    if (MemberDependenciesMet(propKvp.Value.Dependencies, calledMethods))
                    {
                        body.AppendLine($"        {propKvp.Value.Signature}");
                    }
                }
            }

            body.AppendLine();

            foreach (KeyValuePair<string, MethodSource> methodKvp in methods)
            {
                string methodName = methodKvp.Key;
                string signature = methodKvp.Value.Signature;
                string returnStub = methodKvp.Value.ReturnStub;
                string fullName = $"{className}.{methodName}";

                body.AppendLine($"        {signature}");
                if (calledMethods.Contains(fullName))
                {
                    body.AppendLine("        {");
                    body.AppendLine(methodKvp.Value.Implementation);
                    if (returnStub.Length > 0)
                    {
                        body.AppendLine($"            {returnStub}");
                    }

                    body.AppendLine("        }");
                }
                else
                {
                    body.AppendLine("        {");
                    if (returnStub.Length > 0)
                    {
                        body.AppendLine($"            {returnStub}");
                    }

                    body.AppendLine("        }");
                }

                body.AppendLine();
            }

            string code = $@"// <auto-generated/>

namespace {Namespace}
{{
    {kvp.Value.Signature}
    {{
{body.ToString().TrimEnd()}
    }}
}}
";

            context.AddSource($"{className}.g.cs", SourceText.From(code, Encoding.UTF8));
        }
    }

    private static HashSet<string> ExpandDependencies(HashSet<string> calledMethods)
    {
        HashSet<string> expanded = new HashSet<string>(calledMethods);
        Queue<string> queue = new Queue<string>(calledMethods);

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            int dot = current.IndexOf('.');
            if (dot < 0)
            {
                continue;
            }

            string className = current.Substring(0, dot);
            string methodName = current.Substring(dot + 1);

            if (TypesToGenerate.TryGetValue(className, out TypeSource? typeSource)
                && typeSource.Methods.TryGetValue(methodName, out MethodSource? methodSource)
                && methodSource.Dependencies != null)
            {
                foreach (string dep in methodSource.Dependencies)
                {
                    if (expanded.Add(dep))
                    {
                        queue.Enqueue(dep);
                    }
                }
            }
        }

        return expanded;
    }
}