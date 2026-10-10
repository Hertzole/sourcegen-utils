using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bogus;
using Hertzole.SourceGenUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public abstract class GeneratorTests
{
    protected static readonly Faker Fake = new Faker();

    protected const string NAMESPACE = Generator.NAMESPACE;

    [Test]
    public void Exists()
    {
        GeneratorDriverRunResult result = AssertGeneratedOutput<Generator>();
        AssertFileAndShellExists(GetTypeName(), result);
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

    protected static Type CompileGeneratedTypeByUsing(string typeName, string useMethod)
    {
        return AssemblyBuilder.CompileGeneratedTypeByUsing(typeName, useMethod);
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
}