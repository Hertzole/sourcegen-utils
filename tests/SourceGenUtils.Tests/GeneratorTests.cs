using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Bogus;
using Hertzole.SourceGenUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public abstract partial class GeneratorTests
{
    protected static readonly Faker Fake = new Faker();

    [Test]
    public void Exists()
    {
        GeneratorDriverRunResult result = AssertGeneratedOutput<Generator>();
        AssertFileAndShellExists(GetTypeName(), result);
    }

    /// <summary>
    ///     Removes any namespaces from a method name string. For example, My.Namespace.Type.Method(string value) ->
    ///     Method(string value)
    /// </summary>
    protected static ReadOnlySpan<char> GetMethodNameWithArgs(ReadOnlySpan<char> method)
    {
        int parentheses = method.IndexOf('(');

        // Slices from the start to the first '(': My.Namespace.Type.Method(
        ReadOnlySpan<char> firstSlice = method.Slice(0, parentheses);

        // Finds the last dot in the first slice: My.Namespace.Type
        int lastDot = firstSlice.LastIndexOf('.');

        // Slices from the last dot to the end: Type.Method(...)
        ReadOnlySpan<char> methodName = method.Slice(lastDot + 1);

        return methodName;
    }

    /// <summary>
    ///     Removes any namespaces and args from a method name string. For example, My.Namespace.Type.Method(string value) ->
    ///     Method
    /// </summary>
    protected static ReadOnlySpan<char> GetMethodNameWithoutArgs(ReadOnlySpan<char> method)
    {
        int parentheses = method.IndexOf('(');

        // Slices from the start to the first '(': My.Namespace.Type.Method(
        ReadOnlySpan<char> firstSlice = method.Slice(0, parentheses);

        // Finds the last dot in the first slice: My.Namespace.Type
        int lastDot = firstSlice.LastIndexOf('.');

        // Slices from the last dot to the end: Type.Method(...)
        ReadOnlySpan<char> methodName = method.Slice(lastDot + 1, parentheses);

        return methodName;
    }

    public static GeneratorDriverRunResult AssertGeneratedOutput<T>(string[]? sources = null, MetadataReference[]? additionalReferences = null)
        where T : IIncrementalGenerator, new()
    {
        T generator = new T();
        CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        SyntaxTree[] sourceTrees;
        if (sources != null && sources.Length > 0)
        {
            sourceTrees = new SyntaxTree[sources.Length];

            for (int i = 0; i < sources.Length; i++)
            {
                sourceTrees[i] = CSharpSyntaxTree.ParseText(SourceText.From(sources[i], Encoding.UTF8));
            }
        }
        else
        {
            sourceTrees = Array.Empty<SyntaxTree>();
        }

        List<MetadataReference> references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        if (additionalReferences != null)
        {
            references.AddRange(additionalReferences);
        }

        CSharpCompilation compilation =
            CSharpCompilation.Create("Test", sourceTrees, references);

        GeneratorDriverRunResult runResult = driver.RunGenerators(compilation).GetRunResult();

        Assert.That(runResult, Is.Not.Null, "Run result is null.");
        Assert.That(runResult.Diagnostics, Is.Empty, "Generator produced errors.");
        Assert.That(runResult.Results.Any(r => r.Exception != null), Is.False, "Generator threw an exception.");
        Assert.That(runResult.GeneratedTrees.Any(), "Generator did not produce any output.");

        return runResult;
    }

    public static void AssertResultContainsFile(string fileName, GeneratorDriverRunResult result)
    {
        Assert.That(result.GeneratedTrees.Any(t => t.FilePath.EndsWith(fileName)), Is.True,
            $"There were no generated files that matched {fileName}.");
    }

    public static void AssertFileAndShellExists(string fileName, GeneratorDriverRunResult result)
    {
        AssertResultContainsFile($"{fileName}.g.cs", result);
        AssertResultContainsFile($"{fileName}.Shell.g.cs", result);
    }

    protected static string[] AppendTypeIfNeeded(string typeName, string[] methods)
    {
        for (int i = 0; i < methods.Length; i++)
        {
            methods[i] = AppendTypeIfNeeded(typeName, methods[i]);
        }

        return methods;
    }

    protected static string AppendTypeIfNeeded(string typeName, string method)
    {
        ReadOnlySpan<char> span = method.AsSpan();
        int parensIndex = span.IndexOf('(');

        if (parensIndex < 0)
        {
            return method;
        }

        if (!string.IsNullOrEmpty(typeName))
        {
            ReadOnlySpan<char> slice = span.Slice(0, parensIndex);
            int dot = slice.IndexOf('.');
            if (dot < 0)
            {
                // There is no dot. We can assume the user meant a method inside the typeName.
                return AppendNamespace($"{typeName}.{method}");
            }
        }

        return AppendNamespace(method);
    }

    protected static string AppendNamespace(string value)
    {
        if (!value.StartsWith($"{Generator.NAMESPACE}."))
        {
            return Generator.NAMESPACE + "." + value;
        }

        return value;
    }

    protected static Assembly CompileAssembly(params string[] calledMethods)
    {
        return CompileAssembly(null, false, calledMethods);
    }

    protected static Assembly CompileUnsafeAssembly(params string[] calledMethods)
    {
        return CompileAssembly(null, true, calledMethods);
    }

    private static Assembly CompileAssembly(string? typeName, bool allowUnsafe, string[] calledMethods)
    {
        CancellationToken ct = CancellationToken.None;

        typeName ??= string.Empty;

        // Expand method names to full paths
        List<string> calledList = new List<string>(calledMethods.Length);
        for (int i = 0; i < calledMethods.Length; i++)
        {
            if (string.IsNullOrEmpty(calledMethods[i]))
            {
                continue;
            }

            calledList.Add(AppendTypeIfNeeded(typeName, calledMethods[i]));
        }

        // Expand dependencies (constructors etc.)
        HashSet<string> expanded = Generator.ExpandDependencies(new HashSet<string>(calledList), ct);

        // Generate implementation (.g.cs)
        ImplementationContext implementationContext = new ImplementationContext(expanded, ct, false);

        using CodeWriter writer = new CodeWriter();
        writer.AppendNullable();
        writer.AppendNamespace(Generator.NAMESPACE);

        foreach (KeyValuePair<string, TypeSource> source in Generator.TypesToGenerate)
        {
            Generator.AppendType(source.Value, $"{Generator.NAMESPACE}.{source.Key}", writer, in implementationContext);
        }

        string impl = writer.ToString();

        // Generate shell (.Shell.g.cs)
        writer.Clear();
        writer.AppendNullable();
        writer.AppendNamespace(Generator.NAMESPACE);
        foreach (KeyValuePair<string, TypeSource> source in Generator.TypesToGenerate)
        {
            Generator.AppendShellType(writer, source.Value, ct);
        }

        string shell = writer.ToString();

        // Strip EmbeddedAttribute — Roslyn generates this, not a real type
        shell = EmbeddedAttributeRegex().Replace(shell, string.Empty);

        // Compile
        string fullSource = impl + "\n" + shell;
        SyntaxTree tree = CSharpSyntaxTree.ParseText(fullSource, CSharpParseOptions.Default.WithPreprocessorSymbols("DEBUG"));

        MetadataReference[] refs = AppDomain.CurrentDomain.GetAssemblies()
                                            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                                            .Select(a => MetadataReference.CreateFromFile(a.Location))
                                            .ToArray<MetadataReference>();

        CSharpCompilation compilation = CSharpCompilation.Create("test",
            [tree], refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: allowUnsafe));

        using MemoryStream ms = new MemoryStream();
        EmitResult emitResult = compilation.Emit(ms);

        if (!emitResult.Success)
        {
            Assert.Fail(string.Join("\n", emitResult.Diagnostics
                                                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                                                    .Select(d => d.ToString())));
        }

        ms.Position = 0;
        return Assembly.Load(ms.ToArray());
    }

    private static Type CompileGeneratedType(string typeName, bool allowUnsafe, string[] calledMethods)
    {
        Assembly asm = CompileAssembly(typeName, allowUnsafe, calledMethods);
        Type? foundType = asm.GetType($"{Generator.NAMESPACE}.{typeName}");

        Assert.That(foundType, Is.Not.Null, $"Can't find type {Generator.NAMESPACE}.{typeName}");

        return foundType!;
    }

    protected static Type CompileGeneratedType(string typeName, params string[] calledMethods)
    {
        return CompileGeneratedType(typeName, false, calledMethods);
    }

    protected static Type CompileUnsafeGeneratedType(string typeName, params string[] calledMethods)
    {
        return CompileGeneratedType(typeName, true, calledMethods);
    }

    public static object CreateInstance(Type type, params object?[] args)
    {
        object? instance = Activator.CreateInstance(type, args);

        Assert.That(instance, Is.Not.Null, $"Can't create instance from '{type}'");

        return instance!;
    }

    protected static MethodInfo GetMethod(Type type, string name, BindingFlags flags)
    {
        MethodInfo? method = type.GetMethod(name, flags);

        Assert.That(method, Is.Not.Null, $"Can't find method '{name}' in type '{type.FullName}'");
        return method!;
    }

    protected static MethodInfo GetMethod(Type type, string name, BindingFlags flags, params Type[] types)
    {
        MethodInfo? method = type.GetMethod(name, flags, types);

        Assert.That(method, Is.Not.Null, $"Can't find method '{name}' in type '{type.FullName}'");
        return method!;
    }

    protected static FieldInfo GetField(Type type, string name, BindingFlags flags)
    {
        FieldInfo? field = type.GetField(name, flags);

        Assert.That(field, Is.Not.Null, $"Can't find field '{name}' in type '{type.FullName}'");
        return field!;
    }

    protected static PropertyInfo GetProperty(Type type, string name, BindingFlags flags)
    {
        PropertyInfo? property = type.GetProperty(name, flags);

        Assert.That(property, Is.Not.Null, $"Can't find property '{name}' in type '{type.FullName}'");
        return property!;
    }

    protected static string GetTypesString(params Type[] types)
    {
        return types.Length == 0 ? string.Empty : string.Join(", ", types.Select(GetTypeString));
    }

    private static string GetTypeString(Type type)
    {
        if (type.IsArray)
        {
            return GetTypeString(type.GetElementType()!);
        }

        if (type == typeof(string))
        {
            return "string";
        }

        if (type == typeof(byte))
        {
            return "byte";
        }

        if (type == typeof(sbyte))
        {
            return "sbyte";
        }

        if (type == typeof(short))
        {
            return "short";
        }

        if (type == typeof(ushort))
        {
            return "ushort";
        }

        if (type == typeof(int))
        {
            return "int";
        }

        if (type == typeof(uint))
        {
            return "uint";
        }

        if (type == typeof(long))
        {
            return "long";
        }

        if (type == typeof(ulong))
        {
            return "ulong";
        }

        if (type == typeof(float))
        {
            return "float";
        }

        if (type == typeof(double))
        {
            return "double";
        }

        if (type == typeof(decimal))
        {
            return "decimal";
        }

        if (type == typeof(char))
        {
            return "char";
        }

        if (type == typeof(bool))
        {
            return "bool";
        }

        if (type == typeof(object))
        {
            return "object";
        }

        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(type.Namespace))
        {
            sb.Append(type.Namespace);
            sb.Append('.');
        }

        if (type.IsGenericType)
        {
            ReadOnlySpan<char> span = type.Name.AsSpan();
            int genericArgIndex = span.IndexOf("`");

            sb.Append(span.Slice(0, genericArgIndex));
            sb.Append('<');
            for (int i = 0; i < type.GenericTypeArguments.Length; i++)
            {
                sb.Append(GetTypeString(type.GenericTypeArguments[i]));
                if (i < type.GenericTypeArguments.Length - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.Append('>');
        }
        else
        {
            sb.Append(type.Name);
        }

        return sb.ToString();
    }

    protected abstract string GetTypeName();

    [GeneratedRegex(@"[ \t]*\[global::Microsoft\.CodeAnalysis\.EmbeddedAttribute\]\r?\n?")]
    private static partial Regex EmbeddedAttributeRegex();
}