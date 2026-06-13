using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
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

    static Generator()
    {
        AllClassNames = new HashSet<string>(TypesToGenerate.Keys);
        AllMethodNames = new HashSet<string>();
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
    }

    private const string NAMESPACE = "Hertzole.SourceGen";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            foreach (KeyValuePair<string, TypeSource> source in TypesToGenerate)
            {
                ctx.CancellationToken.ThrowIfCancellationRequested();
                GenerateShell(source.Value, source.Key, ctx);
            }
        });

        IncrementalValueProvider<ImmutableArray<string?>> calledMethods =
            context.SyntaxProvider
                   .CreateSyntaxProvider(
                       (s, _) => s is InvocationExpressionSyntax invocation
                                 && invocation.Expression is MemberAccessExpressionSyntax maes
                                 && maes.Name is IdentifierNameSyntax name
                                 && AllMethodNames.Contains(name.Identifier.Text),
                       (ctx, cancelToken) =>
                       {
                           InvocationExpressionSyntax invocation = (InvocationExpressionSyntax) ctx.Node;
                           MemberAccessExpressionSyntax maes = (MemberAccessExpressionSyntax) invocation.Expression;
                           string methodName = maes.Name.Identifier.Text;
                           int argCount = invocation.ArgumentList.Arguments.Count;

                           // Direct type.Method() call — syntactic, works even on first build
                           if (maes.Expression is IdentifierNameSyntax id
                               && AllClassNames.Contains(id.Identifier.Text)
                               && TypesToGenerate.TryGetValue(id.Identifier.Text, out TypeSource? type)
                               && type.ContainsMethod(methodName, cancelToken))
                           {
                               return $"{id.Identifier.Text}.{methodName}:{argCount}";
                           }

                           // Instance call — use semantic model to check the containing type
                           IMethodSymbol? methodSymbol =
                               ctx.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

                           Log.Info($"Symbol: {ctx.SemanticModel.GetSymbolInfo(invocation).Symbol} | Method symbol: {methodSymbol} | {methodSymbol?.ContainingNamespace.ToDisplayString()}");

                           if (methodSymbol?.ContainingNamespace != null && methodSymbol.ContainingNamespace.ToDisplayString() == NAMESPACE)
                           {
                               string containingType = methodSymbol.ContainingType.ToDisplayString();
                               foreach (KeyValuePair<string, TypeSource> kvp in TypesToGenerate)
                               {
                                   if (containingType == $"{NAMESPACE}.{kvp.Key}"
                                       && kvp.Value.ContainsMethod(methodName, cancelToken))
                                   {
                                       return methodSymbol.ToDisplayString();
                                   }
                               }
                           }

                           return null;
                       })
                   .Where(name => name != null)
                   .Collect();

        context.RegisterImplementationSourceOutput(calledMethods,
            (ctx, t) =>
            {
                try
                {
                    // PERF: Pool collections
                    HashSet<string> calledSet = new HashSet<string>(t.Distinct()!);
                    HashSet<string> directCalled = new HashSet<string>(calledSet);
                    calledSet = ExpandDependencies(calledSet, ctx.CancellationToken);
                    GenerateCode(ctx, directCalled, calledSet);
                }

                catch (Exception e)
                {
                    Log.Error(e);
                }
            });
    }

    private static void GenerateShell(TypeSource type, string typeName, in IncrementalGeneratorPostInitializationContext context)
    {
        CodeWriter writer = new CodeWriter();
        writer.AppendNullable();
        writer.AppendNamespace(NAMESPACE);

        if (!string.IsNullOrWhiteSpace(type.ConditionalPreprocessorSymbol))
        {
            writer.AppendConditionalSymbol(type.ConditionalPreprocessorSymbol!);
        }

        writer.AppendLine(type.Signature);

        using (writer.WithBlock())
        {
            for (int i = 0; i < type.Methods.Length; i++)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (type.Methods[i].SkipPartial)
                {
                    continue;
                }

                bool hasConditionalSymbol = !string.IsNullOrWhiteSpace(type.Methods[i].ConditionalPreprocessorSymbol);

                if (hasConditionalSymbol)
                {
                    writer.AppendConditionalSymbol(type.Methods[i].ConditionalPreprocessorSymbol!);
                }

                writer.Append(type.Methods[i].Signature);
                writer.AppendLine(";");

                if (hasConditionalSymbol)
                {
                    writer.AppendPreprocessorSymbol("#endif");
                }

                if (i < type.Methods.Length - 1)
                {
                    writer.AppendLine();
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(type.ConditionalPreprocessorSymbol))
        {
            writer.AppendPreprocessorSymbol("#endif");
        }

        context.AddSource($"{typeName}.Shell.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));
    }

    private static bool AreAnyDependenciesMet(string[]? dependencies, in ImplementationContext context)
    {
        if (dependencies == null || dependencies.Length == 0)
        {
            return true;
        }

        foreach (string dep in dependencies)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (context.HasCalledMethod(dep))
            {
                return true;
            }
        }

        return false;
    }

    private static bool AreAllDependenciesMet(string[]? dependencies, in ImplementationContext context)
    {
        if (dependencies == null || dependencies.Length == 0)
        {
            return true;
        }

        foreach (string dep in dependencies)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (!context.HasCalledMethod(dep))
            {
                return false;
            }
        }

        return true;
    }

    private static void GenerateCode(SourceProductionContext context, HashSet<string> directCalled, HashSet<string> expandedCalled)
    {
        ImplementationContext implementationContext = new ImplementationContext(expandedCalled, context.CancellationToken);

        foreach (KeyValuePair<string, TypeSource> kvp in TypesToGenerate)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            string className = kvp.Key;

            CodeWriter writer = new CodeWriter();

            writer.AppendNullable();
            writer.AppendNamespace(NAMESPACE);

#if DEBUG
            writer.AppendLine($"// Direct called: {directCalled.Count}");
            foreach (string s in directCalled)
            {
                writer.Append("// ");
                writer.AppendLine(s);
            }

            writer.AppendLine();
            writer.AppendLine($"// Expanded called: {expandedCalled.Count}");
            foreach (string s in expandedCalled)
            {
                writer.Append("// ");
                writer.AppendLine(s);
            }

            writer.AppendLine();
            writer.AppendLine($"// Methods: {kvp.Value.Methods.Length}");
            foreach (MethodSource method in kvp.Value.Methods)
            {
                writer.AppendLine($"// {method.Name} ({method.Identifier}): {method.ParameterTypesKey}");
            }
#endif

            AppendType(kvp.Value, $"{NAMESPACE}.{kvp.Key}", writer, expandedCalled, implementationContext);

            context.AddSource($"{className}.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));
        }
    }

    private static void AppendType(TypeSource typeSource, string typeName, CodeWriter writer, HashSet<string> calledMethods, in ImplementationContext implementationContext)
    {
        MethodSource[] methods = typeSource.Methods;

        bool hasConditionalSymbol = !string.IsNullOrWhiteSpace(typeSource.ConditionalPreprocessorSymbol);

        if (hasConditionalSymbol)
        {
            writer.AppendConditionalSymbol(typeSource.ConditionalPreprocessorSymbol!);
        }

        writer.AppendGeneratedCodeAttribute("Hertzole.SourceGenUtils.Generator", "1.0.0.0");
        writer.AppendExcludeFromCodeCoverageAttribute();

        WriteAttributes(typeSource, writer, in implementationContext);

        writer.AppendLine(typeSource.Signature);
        writer.AppendLine("{");
        writer.Indent++;

        if (typeSource.Fields != null)
        {
            foreach (FieldSource field in typeSource.Fields.Values)
            {
                WriteFieldOrProperty(field, writer, in implementationContext);
            }

            writer.AppendLine();
        }

        if (typeSource.Properties != null)
        {
            foreach (PropertySource prop in typeSource.Properties.Values)
            {
                WriteFieldOrProperty(prop, writer, in implementationContext);
            }

            writer.AppendLine();
        }

        // PERF: Pool
        HashSet<Guid> emittedIdentifiers = new HashSet<Guid>();

        foreach (MethodSource method in methods)
        {
            implementationContext.CancellationToken.ThrowIfCancellationRequested();

            if (!emittedIdentifiers.Add(method.Identifier))
            {
                continue;
            }

            string fullName = $"{typeName}.{method.Name}({method.ParameterTypesKey})";
            bool isCalled = calledMethods.Contains(fullName);
#if DEBUG
            writer.AppendLine($"// {fullName}: Is called: {isCalled}");
#endif
            foreach (MethodSource overload in methods)
            {
                implementationContext.CancellationToken.ThrowIfCancellationRequested();

                if (overload.Name != method.Name)
                {
                    // Not an overload as names don't match, skip.
                    continue;
                }

                AppendMethod(typeName, writer, calledMethods, in implementationContext, overload, method);

                emittedIdentifiers.Add(overload.Identifier);
            }
        }

        if (typeSource.Types != null)
        {
            foreach (KeyValuePair<string, TypeSource> typeKvp in typeSource.Types)
            {
                implementationContext.CancellationToken.ThrowIfCancellationRequested();
                AppendType(typeKvp.Value, $"{typeName}.{typeKvp.Key}", writer, calledMethods, in implementationContext);
            }
        }

        writer.Indent--;
        writer.AppendLine("}");

        if (hasConditionalSymbol)
        {
            writer.AppendPreprocessorSymbol("#endif");
        }
    }

    private static void AppendMethod(string typeName, CodeWriter writer, HashSet<string> calledMethods, in ImplementationContext implementationContext, MethodSource overload, MethodSource method)
    {
        string overloadFullName = $"{typeName}.{overload.Name}({overload.ParameterTypesKey})";
        bool isOverloadCalled = calledMethods.Contains(overloadFullName);

        bool hasConditionalSymbol = !string.IsNullOrWhiteSpace(overload.ConditionalPreprocessorSymbol);

        if (hasConditionalSymbol)
        {
            writer.AppendConditionalSymbol(overload.ConditionalPreprocessorSymbol);
        }

#if DEBUG
        writer.AppendLine($"// (overload) {overloadFullName}: Is called: {isOverloadCalled} | Dependencies: {string.Join(", ", overload.Dependencies ?? Array.Empty<string>())}");
#endif
        WriteAttributes(overload, writer, in implementationContext);
        writer.AppendLine(overload.Signature);
        writer.AppendLine("{");
        writer.Indent++;
        if (isOverloadCalled && AreAllDependenciesMet(method.RequiredDependencies, in implementationContext))
        {
            overload.Implementation.Invoke(writer, in implementationContext);
        }
        else if (overload.EmptyStub.Length > 0)
        {
            writer.AppendLine(overload.EmptyStub);
        }

        writer.Indent--;
        writer.AppendLine("}");

        if (hasConditionalSymbol)
        {
            writer.AppendPreprocessorSymbol("#endif");
        }

        writer.AppendLine();
    }

    private static void WriteFieldOrProperty(BaseSource source, CodeWriter writer, in ImplementationContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        if (AreAnyDependenciesMet(source.Dependencies, in context) &&
            AreAllDependenciesMet(source.RequiredDependencies, in context))
        {
            bool hasConditionalSymbol = !string.IsNullOrWhiteSpace(source.ConditionalPreprocessorSymbol);

            if (hasConditionalSymbol)
            {
                writer.AppendConditionalSymbol(source.ConditionalPreprocessorSymbol);
            }

            WriteAttributes(source, writer, in context);

            writer.AppendLine(source.Signature);

            if (hasConditionalSymbol)
            {
                writer.AppendPreprocessorSymbol("#endif");
            }
        }
    }

    private static void WriteAttributes(IHasAttributes source, CodeWriter writer, in ImplementationContext context)
    {
        if (source.Attributes == null || source.Attributes.Length == 0)
        {
            return;
        }

        for (int i = 0; i < source.Attributes.Length; i++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            writer.Append('[');
            writer.Append(source.Attributes[i]);
            writer.AppendLine("]");
        }
    }

    private static HashSet<string> ExpandDependencies(HashSet<string> calledMethods, CancellationToken cancellationToken)
    {
        // PERF: Pool collections
        HashSet<string> expanded = new HashSet<string>(calledMethods);
        Queue<string> queue = new Queue<string>(calledMethods);

        // PERF: Use spans 
        while (queue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string current = queue.Dequeue();
            int namespaceStart = current.IndexOf(NAMESPACE, StringComparison.Ordinal);
            if (namespaceStart >= 0)
            {
                current = current.Substring(namespaceStart + NAMESPACE.Length + 1);
            }

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
                    deps = typeSource.GetMethodDependenciesRecursive(methodPath, paramTypesKey, cancellationToken);
                }
                else
                {
                    deps = typeSource.GetMethodDependenciesRecursive(methodPath, paramCount, cancellationToken);
                }

                if (deps != null)
                {
                    foreach (string dep in deps)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

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