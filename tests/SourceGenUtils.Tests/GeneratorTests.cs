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
internal abstract partial class GeneratorTests
{
    protected readonly Faker Fake = new Faker();

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

    protected static Type CompileGeneratedType(string typeName, params string[] calledMethods)
    {
        CancellationToken ct = CancellationToken.None;
        TypeSource type = Generator.TypesToGenerate[typeName];

        // Expand method names to full paths
        for (int i = 0; i < calledMethods.Length; i++)
        {
            if (!calledMethods[i].StartsWith(Generator.NAMESPACE))
            {
                calledMethods[i] = $"{Generator.NAMESPACE}.{calledMethods[i]}";
            }
        }

        // Expand dependencies (constructors etc.)
        HashSet<string> expanded = Generator.ExpandDependencies(new HashSet<string>(calledMethods), ct);

        // Generate implementation (.g.cs)
        CodeWriter writer = new CodeWriter();
        writer.AppendNullable();
        writer.AppendNamespace(Generator.NAMESPACE);
        Generator.AppendType(type, $"{Generator.NAMESPACE}.{typeName}", writer, new ImplementationContext(expanded, ct, false));
        string impl = writer.ToString();

        // Generate shell (.Shell.g.cs)
        writer.Clear();
        writer.AppendNullable();
        writer.AppendNamespace(Generator.NAMESPACE);
        Generator.AppendShellType(writer, type, ct);
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
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using MemoryStream ms = new MemoryStream();
        EmitResult emitResult = compilation.Emit(ms);

        if (!emitResult.Success)
        {
            Assert.Fail(string.Join("\n", emitResult.Diagnostics
                                                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                                                    .Select(d => d.ToString())));
        }

        ms.Position = 0;
        Assembly asm = Assembly.Load(ms.ToArray());
        Type? foundType = asm.GetType($"{Generator.NAMESPACE}.{typeName}");

        Assert.That(foundType, Is.Not.Null, $"Can't find type {Generator.NAMESPACE}.{typeName}");

        return foundType!;
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

    protected abstract string GetTypeName();

    [GeneratedRegex(@"[ \t]*\[global::Microsoft\.CodeAnalysis\.EmbeddedAttribute\]\r?\n?")]
    private static partial Regex EmbeddedAttributeRegex();
}