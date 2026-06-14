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
    internal static readonly Dictionary<string, TypeSource> TypesToGenerate = new Dictionary<string, TypeSource>
    {
        ["CodeWriter"] = CreateCodeWriter(),
        ["Log"] = CreateLog(),
        ["VariableNames"] = CreateVariableNames(),
        ["EquatableArray"] = CreateEquatableArray(),
        ["SyntaxExtensions"] = CreateSyntaxExtensions()
    };

    private static readonly HashSet<string> AllMethodNames;
    private static readonly Dictionary<string, TypeSource> AllTypes;

    private static SymbolDisplayFormat ContainingTypeDisplayFormat { get; } =
        SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

    static Generator()
    {
        AllMethodNames = new HashSet<string>();
        Dictionary<string, HashSet<string>> typesPerName = new Dictionary<string, HashSet<string>>();
        AllTypes = new Dictionary<string, TypeSource>();

        foreach (KeyValuePair<string, TypeSource> typeKvp in TypesToGenerate)
        {
            CollectType(typeKvp.Key, typeKvp.Value, AllMethodNames, typesPerName);
        }
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
                       (s, _) =>
                       {
                           if (s is InvocationExpressionSyntax invocation
                               && invocation.Expression is MemberAccessExpressionSyntax maes
                               && maes.Name is IdentifierNameSyntax name
                               && AllMethodNames.Contains(name.Identifier.Text))
                           {
                               return true;
                           }

                           if (s is ObjectCreationExpressionSyntax oces
                               && GetSimpleTypeName(oces.Type) is string simpleName
                               && AllMethodNames.Contains(simpleName))
                           {
                               return true;
                           }

                           return false;
                       },
                       (ctx, cancelToken) =>
                       {
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
                               string containingType =
                                   methodSymbol.ContainingType.ToDisplayString(NullableFlowState.NotNull, ContainingTypeDisplayFormat);

                               // Strip generic type arguments (e.g. EquatableArray<char> → EquatableArray)
                               int genericArgIndex = containingType.IndexOf('<');
                               string cleanContainingType =
                                   genericArgIndex >= 0 ? containingType.Substring(0, genericArgIndex) : containingType;

                               Log.Info(
                                   $"Containing type: {containingType} | Clean: {cleanContainingType} | TryGetTypeSource: {AllTypes.TryGetValue(cleanContainingType, out TypeSource? temp)} | Contains method: {temp?.ContainsMethod(methodName, cancelToken)}");

                               if (AllTypes.TryGetValue(cleanContainingType, out TypeSource? typeSource) &&
                                   typeSource.ContainsMethod(methodName, cancelToken))
                               {
                                   // Build the string manually using the original definition to avoid generic type
                                   // arguments (e.g. EquatableArray<char>) in the containing type.
                                   IMethodSymbol originalDef = methodSymbol.OriginalDefinition;
                                   string paramTypes = string.Join(", ", originalDef.Parameters.Select(p => p.Type.ToDisplayString()));
                                   return $"{cleanContainingType}.{methodName}({paramTypes})";
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
                    GenerateCode(ctx, calledSet);
                }

                catch (Exception e)
                {
                    Log.Error(e);
                }
            });
    }

    internal static void GenerateShell(TypeSource type, string typeName, in IncrementalGeneratorPostInitializationContext context)
    {
        CodeWriter writer = new CodeWriter();
        writer.AppendNullable();
        writer.AppendNamespace(NAMESPACE);

        AppendShellType(writer, type, in context);

        context.AddSource($"{typeName}.Shell.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));
    }

    internal static void AppendShellType(CodeWriter writer, TypeSource type, in IncrementalGeneratorPostInitializationContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        bool typeHasConditionalSymbol = !string.IsNullOrWhiteSpace(type.ConditionalPreprocessorSymbol);

        if (typeHasConditionalSymbol)
        {
            writer.AppendConditionalSymbol(type.ConditionalPreprocessorSymbol!);
        }

        writer.AppendLine(type.Signature);

        using (writer.WithBlock())
        {
            if (type.Methods != null && type.Methods.Length > 0)
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

            if (type.Types != null && type.Types.Count > 0)
            {
                foreach (KeyValuePair<string, TypeSource> typeKvp in type.Types)
                {
                    writer.AppendLine();

                    AppendShellType(writer, typeKvp.Value, in context);
                }
            }
        }

        if (typeHasConditionalSymbol)
        {
            writer.AppendPreprocessorSymbol("#endif");
        }
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

        context.AddSource("Debug.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));
#endif
    }

    internal static void AppendType(TypeSource typeSource, string typeName, CodeWriter writer, in ImplementationContext implementationContext)
    {
        MethodSource[]? methods = typeSource.Methods;

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

        if (methods != null && methods.Length > 0)
        {
            foreach (MethodSource method in methods)
            {
                implementationContext.CancellationToken.ThrowIfCancellationRequested();

                writer.AppendLine();

                string fullName = $"{typeName}.{method.Name}({method.ParameterTypesKey})";

                AppendMethod(writer, method, fullName, in implementationContext);
            }
        }

        if (typeSource.Types != null)
        {
            foreach (KeyValuePair<string, TypeSource> typeKvp in typeSource.Types)
            {
                writer.AppendLine();

                implementationContext.CancellationToken.ThrowIfCancellationRequested();
                AppendType(typeKvp.Value, $"{typeName}.{typeKvp.Key}", writer, in implementationContext);
            }
        }

        writer.Indent--;
        writer.AppendLine("}");

        if (hasConditionalSymbol)
        {
            writer.AppendPreprocessorSymbol("#endif");
        }
    }

    internal static void AppendMethod(CodeWriter writer, MethodSource method, string fullName, in ImplementationContext context)
    {
        bool hasConditionalSymbol = !string.IsNullOrWhiteSpace(method.ConditionalPreprocessorSymbol);

        if (hasConditionalSymbol)
        {
            writer.AppendConditionalSymbol(method.ConditionalPreprocessorSymbol);
        }

        WriteAttributes(method, writer, in context);
        writer.AppendLine(method.Signature);
        using (writer.WithBlock())
        {
            if (method.AlwaysWrite || context.HasCalledMethod(fullName) && AreAllDependenciesMet(method.RequiredDependencies, in context))
            {
                method.Implementation.Invoke(writer, in context);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(method.EmptyStub))
                {
                    writer.AppendLine(method.EmptyStub);
                }
            }
        }

        if (hasConditionalSymbol)
        {
            writer.AppendPreprocessorSymbol("#endif");
        }
    }

    internal static void WriteFieldOrProperty(BaseSource source, CodeWriter writer, in ImplementationContext context)
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

            if (source is PropertySource property)
            {
                writer.Append(source.Signature);

                if (property.GetImplementation == null && property.SetImplementation == null)
                {
                    writer.AppendLine(" { get; set; }");
                }
                else
                {
                    writer.AppendLine();

                    using (writer.WithBlock())
                    {
                        if (property.GetImplementation != null)
                        {
                            WriteAttributes(property.GetAttributes, writer, in context);
                            writer.AppendLine("get");

                            using (writer.WithBlock())
                            {
                                property.GetImplementation.Invoke(writer, in context);
                            }
                        }

                        if (property.SetImplementation != null)
                        {
                            WriteAttributes(property.SetAttributes, writer, in context);
                            writer.AppendLine("set");

                            using (writer.WithBlock())
                            {
                                property.SetImplementation.Invoke(writer, in context);
                            }
                        }
                    }
                }
            }
            else
            {
                writer.AppendLine(source.Signature);
            }

            if (hasConditionalSymbol)
            {
                writer.AppendPreprocessorSymbol("#endif");
            }
        }
    }

    private static void WriteAttributes(IHasAttributes source, CodeWriter writer, in ImplementationContext context)
    {
        WriteAttributes(source.Attributes, writer, in context);
    }

    private static void WriteAttributes(string[]? attributes, CodeWriter writer, in ImplementationContext context)
    {
        if (attributes == null || attributes.Length == 0)
        {
            return;
        }

        for (int i = 0; i < attributes.Length; i++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            writer.Append('[');
            writer.Append(attributes[i]);
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