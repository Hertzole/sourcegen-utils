using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Hertzole.SourceGenUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using NUnit.Framework;
using SourceGenUtils.Trimmer;

namespace SourceGenUtils.Tests;

public static class AssemblyBuilder
{
    private const string PREPROCESSOR_SYMBOL =
#if DEBUG
            "DEBUG"
#else
            "RELEASE"
#endif
        ;

    public static GeneratorDriverRunResult GetGeneratorResult(string[] sources,
        bool allowUnsafe,
        out ImmutableArray<Diagnostic> diagnostics,
        out Compilation compilation)
    {
        SyntaxTree[] sourceTrees = sources.Select(static source => ParseSyntaxTree(source)).ToArray();

        return GetGeneratorResult(sourceTrees, allowUnsafe, out diagnostics, out compilation);
    }

    public static SyntaxTree ParseSyntaxTree(string source, string path = "")
    {
        return CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithPreprocessorSymbols(PREPROCESSOR_SYMBOL), path);
    }

    public static GeneratorDriverRunResult GetGeneratorResult(SyntaxTree[] sourceTrees,
        bool allowUnsafe,
        out ImmutableArray<Diagnostic> diagnostics,
        out Compilation compilation)
    {
        Generator generator = new Generator();
        CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() },
            parseOptions: CSharpParseOptions.Default.WithPreprocessorSymbols(PREPROCESSOR_SYMBOL));

        MetadataReference[] refs = AppDomain.CurrentDomain.GetAssemblies()
                                            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                                            .Select(a => MetadataReference.CreateFromFile(a.Location))
                                            .ToArray<MetadataReference>();

        CSharpCompilation c = CSharpCompilation.Create("test",
            sourceTrees, refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: allowUnsafe, nullableContextOptions: NullableContextOptions.Enable));

        return driver.RunGeneratorsAndUpdateCompilation(c, out compilation, out diagnostics).GetRunResult();
    }

    public static Assembly BuildAssemblyWithGenerator(string[] sources, bool allowUnsafe, bool trim)
    {
        GeneratorDriverRunResult runResult = GetGeneratorResult(sources, allowUnsafe, out ImmutableArray<Diagnostic> diagnostics, out Compilation compilation);

        Assert.That(runResult, Is.Not.Null, "Run result is null.");
        Assert.That(runResult.Diagnostics, Is.Empty, "Generator produced errors.");
        Assert.That(runResult.Results.Any(r => r.Exception != null), Is.False, "Generator threw an exception.");
        Assert.That(runResult.GeneratedTrees.Any(), "Generator did not produce any output.");
        Assert.That(diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error), Is.False, "Errors during compilation");

        for (int i = 0; i < runResult.GeneratedTrees.Length; i++)
        {
            // Console.WriteLine(runResult.GeneratedTrees[i].ToString());
        }

        using MemoryStream ms = new MemoryStream();
        EmitResult emitResult = compilation.Emit(ms);

        if (!emitResult.Success)
        {
            Assert.Fail(string.Join("\n", emitResult.Diagnostics
                                                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                                                    .Select(d => d.ToString())));
        }

        ms.Position = 0;

        string tmp = Path.GetTempFileName();
        File.WriteAllBytes(tmp, ms.ToArray());

        if (trim)
        {
            AssemblyTrimmer trimmer = new AssemblyTrimmer();
            trimmer.Trim(tmp);
        }

        return Assembly.Load(File.ReadAllBytes(tmp));
    }

    public static Assembly CompileAssemblyByUsing(string usage, bool compileUnsafe = false)
    {
        using CodeWriter writer = new CodeWriter();

        writer.AppendLine("using Hertzole.SourceGen;");
        writer.AppendLine("using System;");
        writer.AppendLine("using System.Collections;");
        writer.AppendLine("using System.Collections.Generic;");
        writer.AppendNamespace("TestNamespace");
        writer.AppendLine("public class TestClass");
        using (writer.WithBlock())
        {
            writer.AppendLine("public static void TestMethod()");
            using (writer.WithBlock())
            {
                writer.AppendLine(usage);
            }

            writer.AppendLine("public static void Use(params object[] values) { }");
        }

        return BuildAssemblyWithGenerator([writer.ToString()], compileUnsafe, true);
    }

    public static Type CompileGeneratedTypeByUsing(string typeName, string usage, bool compileUnsafe = false)
    {
        Assembly assembly = CompileAssemblyByUsing(usage, compileUnsafe);

        return assembly.GetType($"{Generator.NAMESPACE}.{typeName}", true)!;
    }
}