using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Hertzole.SourceGenUtils;

[Generator]
public sealed partial class Generator : IIncrementalGenerator
{
    private static readonly Dictionary<string, TypeSource> TypesToGenerate = new Dictionary<string, TypeSource>
    {
        ["CodeWriter"] = CreateCodeWriter()
    };

    private static readonly HashSet<string> AllClassNames;
    private static readonly HashSet<string> AllMethodNames;
    private static readonly Dictionary<string, string> MethodToUniqueType;

    static Generator()
    {
        AllClassNames = new HashSet<string>(TypesToGenerate.Keys);
        AllMethodNames = new HashSet<string>();
        MethodToUniqueType = new Dictionary<string, string>();
        Dictionary<string, HashSet<string>> typesPerName = new Dictionary<string, HashSet<string>>();

        foreach (KeyValuePair<string, TypeSource> typeKvp in TypesToGenerate)
        {
            foreach (MethodSource method in typeKvp.Value.Methods)
            {
                AllMethodNames.Add(method.Name);
                if (!typesPerName.TryGetValue(method.Name, out HashSet<string>? types))
                {
                    types = new HashSet<string>();
                    typesPerName[method.Name] = types;
                }

                types.Add(typeKvp.Key);
            }
        }

        foreach (KeyValuePair<string, HashSet<string>> kvp in typesPerName)
        {
            if (kvp.Value.Count == 1)
            {
                MethodToUniqueType[kvp.Key] = kvp.Value.First();
            }
        }
    }

    private const string NAMESPACE = "Hertzole.SourceGen";

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
                           int argCount = invocation.ArgumentList.Arguments.Count;

                           // Direct type.Method() call — syntactic, works even on first build
                           if (maes.Expression is IdentifierNameSyntax id
                               && AllClassNames.Contains(id.Identifier.Text)
                               && TypesToGenerate.TryGetValue(id.Identifier.Text, out TypeSource? type)
                               && type.ContainsMethod(methodName))
                           {
                               return $"{id.Identifier.Text}.{methodName}:{argCount}";
                           }

                           // Instance call — use semantic model to check the containing type
                           IMethodSymbol? methodSymbol =
                               ctx.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

                           if (methodSymbol?.ContainingType != null)
                           {
                               string containingType = methodSymbol.ContainingType.ToDisplayString();
                               foreach (KeyValuePair<string, TypeSource> kvp in TypesToGenerate)
                               {
                                   if (containingType == $"{NAMESPACE}.{kvp.Key}"
                                       && kvp.Value.ContainsMethod(methodName))
                                   {
                                       if (methodSymbol.Parameters.Length == 0)
                                       {
                                           return $"{kvp.Key}.{methodName}";
                                       }

                                       string paramKey = string.Join(",", methodSymbol.Parameters.Select(p => p.Type.ToDisplayString()));
                                       return $"{kvp.Key}.{methodName}({paramKey})";
                                   }
                               }
                           }

                           // Fallback: if the method name is unique to one generated type,
                           // assume this invocation targets it (handles first-build instance calls).
                           if (MethodToUniqueType.TryGetValue(methodName, out string? uniqueType))
                           {
                               return $"{uniqueType}.{methodName}:{argCount}";
                           }

                           return null;
                       })
                   .Where(name => name != null)
                   .Collect();

        context.RegisterImplementationSourceOutput(calledMethods,
            (ctx, t) =>
            {
                HashSet<string> calledSet = new HashSet<string>(t.Distinct()!);
                HashSet<string> directCalled = new HashSet<string>(calledSet);
                calledSet = ExpandDependencies(calledSet);
                GenerateCode(ctx, directCalled, calledSet);
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
            if (calledMethods.Contains(dep))
            {
                return true;
            }

            // Match overload-specific keys like "Type.Method:N" or "Type.Method(string)"
            foreach (string called in calledMethods)
            {
                if (called.StartsWith(dep + ":", StringComparison.Ordinal)
                    || called.StartsWith(dep + "(", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void GenerateCode(SourceProductionContext context, HashSet<string> directCalled, HashSet<string> expandedCalled)
    {
        foreach (KeyValuePair<string, TypeSource> kvp in TypesToGenerate)
        {
            string className = kvp.Key;

            CodeWriter writer = new CodeWriter();

            writer.AppendNullable();
            writer.AppendNamespace(NAMESPACE);

            AppendType(kvp.Value, kvp.Key, writer, expandedCalled, directCalled);

            context.AddSource($"{className}.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));
        }
    }

    private static void AppendType(TypeSource typeSource, string typeName, CodeWriter writer, HashSet<string> calledMethods, HashSet<string>? directCalled = null)
    {
        MethodSource[] methods = typeSource.Methods;
        writer.AppendLine(typeSource.Signature);
        writer.AppendLine("{");
        writer.Indent++;

        if (typeSource.Fields != null)
        {
            foreach (KeyValuePair<string, FieldSource> fieldKvp in typeSource.Fields)
            {
                if (MemberDependenciesMet(fieldKvp.Value.Dependencies, calledMethods))
                {
                    writer.AppendLine(fieldKvp.Value.Signature);
                }
            }

            writer.AppendLine();
        }

        if (typeSource.Properties != null)
        {
            foreach (KeyValuePair<string, PropertySource> propKvp in typeSource.Properties)
            {
                if (MemberDependenciesMet(propKvp.Value.Dependencies, calledMethods))
                {
                    writer.AppendLine(propKvp.Value.Signature);
                }
            }

            writer.AppendLine();
        }

        HashSet<string> emittedNames = new HashSet<string>();

        foreach (MethodSource method in methods)
        {
            if (!emittedNames.Add(method.Name))
            {
                continue;
            }

            string fullName = $"{typeName}.{method.Name}";
            bool isCalled = calledMethods.Contains(fullName);

            foreach (MethodSource overload in methods)
            {
                if (overload.Name != method.Name)
                {
                    continue;
                }

                int pc = overload.ParameterCount;
                string typesKey = pc > 0 ? $"{typeName}.{overload.Name}({overload.ParameterTypesKey})" : fullName;
                string overloadKey = pc >= 0 ? $"{typeName}.{overload.Name}:{pc}" : fullName;
                bool isOverloadCalled = calledMethods.Contains(typesKey) || calledMethods.Contains(overloadKey) || calledMethods.Contains(fullName);

                // Also treat as called when any dependency is directly called (field-like behavior)
                if (!isOverloadCalled && overload.Dependencies != null && directCalled != null)
                {
                    isOverloadCalled = MemberDependenciesMet(overload.Dependencies, directCalled);
                }

                writer.AppendLine(overload.Signature);
                writer.AppendLine("{");
                writer.Indent++;
                if (isOverloadCalled)
                {
                    overload.Implementation.Invoke(writer);
                }
                else if (overload.EmptyStub.Length > 0)
                {
                    writer.AppendLine(overload.EmptyStub);
                }

                writer.Indent--;
                writer.AppendLine("}");
                writer.AppendLine();
            }
        }

        if (typeSource.Types != null)
        {
            foreach (KeyValuePair<string, TypeSource> typeKvp in typeSource.Types)
            {
                AppendType(typeKvp.Value, $"{typeName}.{typeKvp.Key}", writer, calledMethods, directCalled);
            }
        }

        writer.Indent--;
        writer.AppendLine("}");
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
            string rest = current.Substring(dot + 1);

            string methodPath;
            int? paramCount = null;
            string? paramTypesKey = null;

            int openParen = rest.IndexOf('(');
            if (openParen >= 0)
            {
                methodPath = rest.Substring(0, openParen);
                int closeParen = rest.LastIndexOf(')');
                if (closeParen > openParen)
                {
                    paramTypesKey = rest.Substring(openParen + 1, closeParen - openParen - 1);
                }
            }
            else
            {
                int colon = rest.IndexOf(':');
                if (colon >= 0 && int.TryParse(rest.Substring(colon + 1), out int pc))
                {
                    methodPath = rest.Substring(0, colon);
                    paramCount = pc;
                }
                else
                {
                    methodPath = rest;
                }
            }

            if (TypesToGenerate.TryGetValue(className, out TypeSource? typeSource))
            {
                string[]? deps;
                if (paramTypesKey != null)
                {
                    deps = typeSource.GetMethodDependenciesRecursive(methodPath, paramTypesKey);
                }
                else
                {
                    deps = typeSource.GetMethodDependenciesRecursive(methodPath, paramCount);
                }

                if (deps != null)
                {
                    foreach (string dep in deps)
                    {
                        if (expanded.Add(dep))
                        {
                            queue.Enqueue(dep);
                        }
                    }
                }
            }
        }

        return expanded;
    }
}