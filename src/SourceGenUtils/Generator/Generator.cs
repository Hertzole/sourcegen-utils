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
    private static readonly string[] AggressiveInlineAttribute =
        ["global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)"];

    internal static readonly Dictionary<string, TypeSource> TypesToGenerate;

    private static readonly HashSet<string> AllMethodNames;
    private static readonly Dictionary<string, TypeSource> AllTypes;

    internal static readonly string generatorName = "Hertzole.SourceGenUtils.Generator";
    internal static readonly string generatorVersion = typeof(Generator).Assembly.GetName().Version.ToString();

    private static SymbolDisplayFormat ContainingTypeDisplayFormat { get; } =
        SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

    static Generator()
    {
        AllMethodNames = new HashSet<string>();
        Dictionary<string, HashSet<string>> typesPerName = new Dictionary<string, HashSet<string>>();
        AllTypes = new Dictionary<string, TypeSource>();

        // Must create it here so any static fields will be initialized first.
        TypesToGenerate = new Dictionary<string, TypeSource>
        {
            ["CodeWriter"] = CreateCodeWriter(),
            ["Log"] = CreateLog(),
            ["VariableNames"] = CreateVariableNames(),
            ["EquatableArray"] = CreateEquatableArray(),
            ["SyntaxExtensions"] = CreateSyntaxExtensions(),
            ["PoolScope"] = CreatePoolScope(),
            ["ObjectPool"] = CreateObjectPool(),
            ["ListPool"] = CreateListPool(),
            ["HashSetPool"] = CreateHashSetPool(),
            ["StackPool"] = CreateStackPool(),
            ["QueuePool"] = CreateQueuePool(),
            ["StringBuilderPool"] = CreateStringBuilderPool(),
            ["ArrayBuilder"] = CreateArrayBuilder(),
            ["ArrayBuilderExtensions"] = CreateArrayBuilderExtensions(),
            ["ContextExtensions"] = CreateContextExtensions()
        };

        foreach (KeyValuePair<string, TypeSource> typeKvp in TypesToGenerate)
        {
            CollectType(typeKvp.Key, typeKvp.Value, AllMethodNames, typesPerName);
        }

        AllMethodNames.TrimExcess();
        typesPerName.Clear();
    }

    private static void CollectType(string typeName,
        TypeSource type,
        HashSet<string> methodNames,
        Dictionary<string, HashSet<string>> typesPerName)
    {
        CollectTypes($"{NAMESPACE}.{typeName}", type);

        CollectMethods(typeName, type, methodNames, typesPerName);

        if (type.Types != null && type.Types.Count > 0)
        {
            foreach (KeyValuePair<string, TypeSource> source in type.Types)
            {
                CollectMethods($"{typeName}.{source.Key}", source.Value, methodNames, typesPerName);
            }
        }
    }

    private static void CollectMethods(string typeName,
        TypeSource type,
        HashSet<string> methodNames,
        Dictionary<string, HashSet<string>> typesPerName)
    {
        if (type.Methods == null || type.Methods.Length == 0)
        {
            return;
        }

        foreach (MethodSource method in type.Methods)
        {
            methodNames.Add(method.Name);
            Log.Info($"All method names: {method.Name}");
            if (!typesPerName.TryGetValue(method.Name, out HashSet<string>? types))
            {
                types = new HashSet<string>();
                typesPerName[method.Name] = types;
            }

            types.Add(typeName);
        }
    }

    private static void CollectTypes(string fullName, TypeSource type)
    {
        Log.Info($"Collecting {fullName}");

        AllTypes[fullName] = type;

        if (type.Types != null)
        {
            foreach (KeyValuePair<string, TypeSource> kvp in type.Types)
            {
                CollectTypes($"{fullName}.{kvp.Key}", kvp.Value);
            }
        }
    }

    private static string? GetSimpleTypeName(TypeSyntax type)
    {
        while (true)
        {
            if (type is IdentifierNameSyntax ins)
            {
                return ins.Identifier.Text;
            }

            if (type is GenericNameSyntax gns)
            {
                return gns.Identifier.Text;
            }

            if (type is QualifiedNameSyntax qns)
            {
                type = qns.Right;
                continue;
            }

            return null;
        }
    }

    internal const string NAMESPACE = "Hertzole.SourceGen";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ExecuteInitialization);

        IncrementalValueProvider<ImmutableArray<string?>> calledMethods =
            context.SyntaxProvider
                   .CreateSyntaxProvider(IsValidSyntaxNode, TransformNodeToCalledMethod)
                   .Where(name => name != null)
                   .Collect();

        context.RegisterImplementationSourceOutput(calledMethods, Execute);
    }

    private static bool IsValidSyntaxNode(SyntaxNode s, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (IsInvocation(s))
        {
            return true;
        }

        if (IsObjectCreation(s))
        {
            return true;
        }

        return false;

        static bool IsInvocation(SyntaxNode node)
        {
            if (node is not InvocationExpressionSyntax invocation)
            {
                return false;
            }

            if (invocation.Expression is not MemberAccessExpressionSyntax maes)
            {
                return false;
            }

            if (maes.Name is not IdentifierNameSyntax name)
            {
                return false;
            }

            return AllMethodNames.Contains(name.Identifier.Text);
        }

        static bool IsObjectCreation(SyntaxNode node)
        {
            if (node is not ObjectCreationExpressionSyntax oces)
            {
                return false;
            }

            if (GetSimpleTypeName(oces.Type) is not string simpleName)
            {
                return false;
            }

            return AllMethodNames.Contains(simpleName);
        }
    }

    private static string? TransformNodeToCalledMethod(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string methodName;
        IMethodSymbol? methodSymbol;

        if (ctx.Node is InvocationExpressionSyntax invocation)
        {
            MemberAccessExpressionSyntax maes = (MemberAccessExpressionSyntax) invocation.Expression;
            methodName = maes.Name.Identifier.Text;
            methodSymbol = ctx.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        }
        else if (ctx.Node is ObjectCreationExpressionSyntax oces)
        {
            string? simpleName = GetSimpleTypeName(oces.Type);
            if (simpleName == null)
            {
                return null;
            }

            methodName = simpleName;
            methodSymbol = ctx.SemanticModel.GetSymbolInfo(oces).Symbol as IMethodSymbol;
        }
        else
        {
            return null;
        }

        Log.Info(
            $"Symbol: {ctx.SemanticModel.GetSymbolInfo(ctx.Node).Symbol} | Method symbol: {methodSymbol} | {methodSymbol?.ContainingNamespace.ToDisplayString()}");

        if (methodSymbol?.ContainingNamespace != null && methodSymbol.ContainingNamespace.ToDisplayString() == NAMESPACE)
        {
            // Check if the containing type is one of the generator's types.
            // Then check if the containing type contains a method with this method name.
            string containingType = methodSymbol.ContainingType.ToDisplayString(NullableFlowState.NotNull, ContainingTypeDisplayFormat);

            // Strip generic type arguments (e.g. EquatableArray<char> → EquatableArray)
            int genericArgIndex = containingType.IndexOf('<');
            string cleanContainingType = genericArgIndex >= 0 ? containingType.Substring(0, genericArgIndex) : containingType;

            Log.Info(
                $"Containing type: {containingType} | Clean: {cleanContainingType} | TryGetTypeSource: {AllTypes.TryGetValue(cleanContainingType, out TypeSource? temp)} | Contains method: {temp?.ContainsMethod(methodName, cancellationToken)}");

            if (AllTypes.TryGetValue(cleanContainingType, out TypeSource? typeSource) && typeSource.ContainsMethod(methodName, cancellationToken))
            {
                // Build the string manually using the original definition to avoid generic type
                // arguments (e.g. EquatableArray<char>) in the containing type.
                // Use ReducedFrom for reduced extension methods to include the 'this' parameter.
                IMethodSymbol originalDef = methodSymbol.ReducedFrom ?? methodSymbol.OriginalDefinition;
                string paramTypes = string.Join(", ", originalDef.Parameters.Select(p => p.Type.ToDisplayString()));
                return $"{cleanContainingType}.{methodName}({paramTypes})";
            }
        }

        return null;
    }

    private static void ExecuteInitialization(IncrementalGeneratorPostInitializationContext ctx)
    {
        ctx.AddEmbeddedAttributeDefinition();

        foreach (KeyValuePair<string, TypeSource> source in TypesToGenerate)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();
            GenerateShell(source.Value, source.Key, ctx);
        }
    }

    private static void Execute(SourceProductionContext ctx, ImmutableArray<string?> t)
    {
        try
        {
            // PERF: Pool collections
            HashSet<string> calledSet = new HashSet<string>(t);
            calledSet = ExpandDependencies(calledSet, ctx.CancellationToken);
            GenerateCode(ctx, calledSet);
        }

        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    internal static void GenerateShell(TypeSource type, string typeName, in IncrementalGeneratorPostInitializationContext context)
    {
        CodeWriter writer = new CodeWriter();
        writer.AppendNullable();
        writer.AppendNamespace(NAMESPACE);

        AppendShellType(writer, type, context.CancellationToken);

        context.AddSource($"{typeName}.Shell.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));
    }

    private static void GenerateCode(SourceProductionContext context, HashSet<string> calledMethods)
    {
        ImplementationContext implementationContext = new ImplementationContext(calledMethods, context.CancellationToken);

        CodeWriter writer = new CodeWriter();

        foreach (KeyValuePair<string, TypeSource> kvp in TypesToGenerate)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            string className = kvp.Key;

            writer.Clear();

            writer.AppendNullable();
            writer.AppendNamespace(NAMESPACE);

            AppendType(kvp.Value, $"{NAMESPACE}.{kvp.Key}", writer, implementationContext);

            context.AddSource($"{className}.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));
        }

#if DEBUG
        writer.Clear();
        writer.AppendLine($"// Called methods: {calledMethods.Count}");
        foreach (string calledMethod in calledMethods)
        {
            writer.AppendLine($"// {calledMethod}");
        }

        writer.AppendLine("\n// Types:");
        foreach (KeyValuePair<string, TypeSource> pair in TypesToGenerate)
        {
            AppendDebugType(pair.Value, writer, pair.Key, NAMESPACE, 1);
        }

        context.AddSource("Debug.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));

        static void AppendDebugType(TypeSource type, CodeWriter writer, string name, string path, int indent)
        {
            writer.Append("// ");
            writer.Append(' ', indent * 4);
            writer.Append(path);
            writer.Append('.');
            writer.AppendLine(name);
            if (type.Methods != null)
            {
                foreach (MethodSource method in type.Methods)
                {
                    writer.Append("// ");
                    writer.Append(' ', indent * 4 + 4);
                    writer.AppendLine($"{path}.{name}.{method.Name}({method.ParameterTypesKey})");
                }
            }

            if (type.Types != null)
            {
                writer.Append("// ");
                writer.Append(' ', indent * 4);
                writer.AppendLine("Nested types:");
                foreach (KeyValuePair<string, TypeSource> pair in type.Types)
                {
                    AppendDebugType(pair.Value, writer, pair.Key, path + "." + name, indent + 1);
                }
            }
        }
#endif
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

    private static HashSet<string> ExpandDependencies(HashSet<string> calledMethods, CancellationToken cancellationToken)
    {
        // PERF: Pool collections
        HashSet<string> expanded = new HashSet<string>(calledMethods);
        Queue<string> queue = new Queue<string>(calledMethods);
        List<string>? deps = null;

        while (queue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ReadOnlySpan<char> current = queue.Dequeue().AsSpan();
            int namespaceStart = current.IndexOf(NAMESPACE, StringComparison.Ordinal);
            if (namespaceStart >= 0)
            {
                current = current.Slice(namespaceStart + NAMESPACE.Length + 1);
            }

            int dot = current.IndexOf('.');
            if (dot < 0)
            {
                continue;
            }

            ReadOnlySpan<char> className = current.Slice(0, dot);
            ReadOnlySpan<char> rest = current.Slice(dot + 1);

            ReadOnlySpan<char> methodPath;
            int? paramCount = null;
            ReadOnlySpan<char> paramTypesKey = null;
            bool hasParamTypeKey = false;

            int openParen = rest.IndexOf('(');
            if (openParen >= 0)
            {
                methodPath = rest.Slice(0, openParen);
                int closeParen = rest.LastIndexOf(')');
                if (closeParen > openParen)
                {
                    paramTypesKey = rest.Slice(openParen + 1, closeParen - openParen - 1);
                    hasParamTypeKey = true;
                }
            }
            else
            {
                int colon = rest.IndexOf(':');
                if (colon >= 0 && int.TryParse(rest.Slice(colon + 1).ToString(), out int pc))
                {
                    methodPath = rest.Slice(0, colon);
                    paramCount = pc;
                }
                else
                {
                    methodPath = rest;
                }
            }

            if (TypesToGenerate.TryGetValue(className.ToString(), out TypeSource? typeSource))
            {
                deps ??= new List<string>();
                deps.Clear();

                if (hasParamTypeKey)
                {
                    typeSource.GetMethodDependenciesRecursive(methodPath, paramTypesKey, deps, cancellationToken);
                }
                else
                {
                    typeSource.GetMethodDependenciesRecursive(methodPath.ToString(), paramCount, deps, cancellationToken);
                }

                if (deps.Count > 0)
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