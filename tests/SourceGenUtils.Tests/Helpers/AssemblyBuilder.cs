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
    public static Assembly BuildAssemblyWithGenerator(string[] sources, bool allowUnsafe, bool trim)
    {
        const string preprocessor_symbol =
#if DEBUG
                "DEBUG"
#else
                "RELEASE"
#endif
            ;

        Generator generator = new Generator();
        CSharpGeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() },
            parseOptions: CSharpParseOptions.Default.WithPreprocessorSymbols(preprocessor_symbol));

        SyntaxTree[] sourceTrees = sources.Select(static source =>
            CSharpSyntaxTree.ParseText(source, CSharpParseOptions.Default.WithPreprocessorSymbols(preprocessor_symbol))).ToArray();

        MetadataReference[] refs = AppDomain.CurrentDomain.GetAssemblies()
                                            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                                            .Select(a => MetadataReference.CreateFromFile(a.Location))
                                            .ToArray<MetadataReference>();

        CSharpCompilation compilation = CSharpCompilation.Create("test",
            sourceTrees, refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: allowUnsafe));

        GeneratorDriverRunResult runResult = driver
                                             .RunGeneratorsAndUpdateCompilation(compilation, out Compilation newCompilation,
                                                 out ImmutableArray<Diagnostic> diagnostics).GetRunResult();

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
        EmitResult emitResult = newCompilation.Emit(ms);

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

    public static Assembly CompileAssemblyByUsing(string usage)
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
        }

        return BuildAssemblyWithGenerator([writer.ToString()], false, true);
    }

    public static Type CompileGeneratedTypeByUsing(string typeName, string usage)
    {
        var assembly = CompileAssemblyByUsing(usage);

        return assembly.GetType($"{Generator.NAMESPACE}.{typeName}", true)!;
    }
}