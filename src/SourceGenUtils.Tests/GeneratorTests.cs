using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Hertzole.SourceGenUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

[TestFixture]
internal abstract class GeneratorTests
{
    [Test]
    public void Exists()
    {
        GeneratorDriverRunResult result = AssertGeneratedOutput<Generator>();
        AssertFileAndShellExists(GetTypeName(), result);
    }

    [Test]
    public void NoCalls_GeneratesDefault()
    {
        GeneratorDriverRunResult result = AssertGeneratedOutput<Generator>();
        AssertFileAndShellExists(GetTypeName(), result);

        SyntaxTree content = result.GeneratedTrees.Single(t => t.FilePath.EndsWith($"{GetTypeName()}.g.cs"));
        SyntaxTree shell = result.GeneratedTrees.Single(t => t.FilePath.EndsWith($"{GetTypeName()}.Shell.g.cs"));

        Assert.That(content.ToString(), Is.EqualTo(GetTypeContent(Generator.TypesToGenerate[GetTypeName()], GetTypeName())));
        Assert.That(shell.ToString(), Is.EqualTo(GetTypeShellContent(Generator.TypesToGenerate[GetTypeName()])));
    }

    protected string GetTypeContent(params string[]? calledMethods)
    {
        return GetTypeContent(Generator.TypesToGenerate[GetTypeName()], GetTypeName(), calledMethods);
    }

    protected static string GetTypeContent(TypeSource type, string typeName, string[]? calledMethods = null)
    {
        CodeWriter writer = new CodeWriter();
        writer.AppendNullable();
        writer.AppendNamespace(Generator.NAMESPACE);

        if (!typeName.StartsWith(Generator.NAMESPACE))
        {
            typeName = $"{Generator.NAMESPACE}.{typeName}";
        }

        Generator.AppendType(type, typeName, writer, new ImplementationContext(GetCalledMethods(calledMethods), CancellationToken.None));

        return writer.ToString();
    }

    protected static string GetTypeShellContent(TypeSource type)
    {
        CodeWriter writer = new CodeWriter();
        writer.AppendNullable();
        writer.AppendNamespace(Generator.NAMESPACE);

        Generator.AppendShellType(writer, type, new IncrementalGeneratorPostInitializationContext());

        return writer.ToString();
    }

    public static GeneratorDriverRunResult AssertGeneratedOutput<T>(string source) where T : IIncrementalGenerator, new()
    {
        return AssertGeneratedOutput<T>([source]);
    }

    public static GeneratorDriverRunResult AssertGeneratedOutput<T>(string[]? sources = null) where T : IIncrementalGenerator, new()
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

        CSharpCompilation compilation =
            CSharpCompilation.Create("Test", sourceTrees, [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

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

    public void AssertGenerateTypeHasContent(string expected, GeneratorDriverRunResult result)
    {
        SyntaxTree? generatedFile = result.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith($"{GetTypeName()}.g.cs"));
        Assert.That(generatedFile, Is.Not.Null, "Generated file is null.");
        Assert.That(generatedFile!.GetText().ToString(), Is.EqualTo(expected), "Generated file content does not match expected.");
    }

    protected static string GenerateCall(Action<CodeWriter> write, string[]? usings = null)
    {
        CodeWriter writer = new CodeWriter();
        writer.AppendNullable();

        writer.AppendLine("using Hertzole.SourceGen;");
        if (usings != null && usings.Length > 0)
        {
            for (int i = 0; i < usings.Length; i++)
            {
                writer.AppendLine($"using {usings[i]};");
            }
        }

        writer.AppendLine("public class TestMethodCaller");
        using (writer.WithBlock())
        {
            writer.AppendLine("public void TestMethod()");
            using (writer.WithBlock())
            {
                write.Invoke(writer);
            }
        }

        return writer.ToString();
    }

    public static string GetMethodContent(string path, params string[] calledMethods)
    {
        MethodSource method = GetMethod(path);

        CodeWriter writer = new CodeWriter();

        string fullName = !path.StartsWith(Generator.NAMESPACE) ? $"{Generator.NAMESPACE}.{path}" : path;

        List<string> calls = new List<string>(calledMethods)
        {
            fullName
        };

        Generator.AppendMethod(writer, method, fullName,
            new ImplementationContext(GetCalledMethods(calls.ToArray()), CancellationToken.None));

        return writer.ToString();
    }

    public static MethodSource GetMethod(string path)
    {
        if (path.StartsWith(Generator.NAMESPACE))
        {
            path = path.Substring(Generator.NAMESPACE.Length + 1);
        }

        TypeSource? currentType = null;
        bool first = true;

        int tries = 0;

        do
        {
            int index = path.IndexOf('.');
            if (index != -1)
            {
                string typeName = path.Substring(0, index);
                if (first)
                {
                    currentType = Generator.TypesToGenerate[typeName];
                    first = false;
                }
                else
                {
                    Assert.That(currentType, Is.Not.Null, "currentType should have been populated by now.");
                    Assert.That(currentType!.Types, Is.Not.Null, $"There are no more types in {typeName}.");

                    currentType = currentType.Types![typeName];
                }
            }
            else
            {
                Assert.That(currentType!.Methods, Is.Not.Null, "There are no methods.");

                MethodSource? method = currentType.Methods!.FirstOrDefault(x => $"{x.Name}({x.ParameterTypesKey})" == path);
                Assert.That(method, Is.Not.Null, $"There's no method with the path {path}.");
                return method!;
            }

            path = path.Substring(index + 1);
        } while (tries++ < 100);

        throw new ArgumentException($"Could not find method {path}.");
    }

    private static HashSet<string> GetCalledMethods(string[]? calledMethods)
    {
        if (calledMethods != null)
        {
            for (int i = 0; i < calledMethods.Length; i++)
            {
                if (!calledMethods[i].StartsWith(Generator.NAMESPACE))
                {
                    calledMethods[i] = Generator.NAMESPACE + "." + calledMethods[i];
                }
            }
        }

        return new HashSet<string>(calledMethods ?? Array.Empty<string>());
    }

    protected abstract string GetTypeName();
}