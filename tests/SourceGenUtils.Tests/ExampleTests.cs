using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Hertzole.SourceGenUtils;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

public partial class ExampleTests
{
    [GeneratedRegex("<code>(.*?)</code>", RegexOptions.Singleline)]
    private static partial Regex ExampleCodeRegex();

    [Test]
    public void TypeExamplesAreValid([Values] bool allowUnsafe)
    {
        // Arrange
        SyntaxTree[] examples = ForEachType(static (name, source, examples) => { AppendExample(name, examples, source.Trivia); });

        // Act
        AssemblyBuilder.GetGeneratorResult(examples, allowUnsafe, out _, out Compilation compilation);

        // Assert
        AssertDiagnostics(compilation);
    }

    [Test]
    public void FieldExamplesAreValid([Values] bool allowUnsafe)
    {
        // Arrange
        SyntaxTree[] examples = ForEachType(static (name, source, examples) =>
        {
            if (source.Fields is Dictionary<string, FieldSource> fields)
            {
                foreach ((string fieldName, FieldSource field) in fields)
                {
                    AppendExample($"{name}.{fieldName}", examples, field.Trivia);
                }
            }
        });

        // Act
        AssemblyBuilder.GetGeneratorResult(examples, allowUnsafe, out _, out Compilation compilation);

        // Assert
        AssertDiagnostics(compilation);
    }

    [Test]
    public void PropertyExamplesAreValid([Values] bool allowUnsafe)
    {
        // Arrange
        SyntaxTree[] examples = ForEachType(static (name, source, examples) =>
        {
            if (source.Properties is Dictionary<string, PropertySource> properties)
            {
                foreach ((string propertyName, PropertySource property) in properties)
                {
                    AppendExample($"{name}.{propertyName}", examples, property.Trivia);
                }
            }
        });

        // Act
        AssemblyBuilder.GetGeneratorResult(examples, allowUnsafe, out _, out Compilation compilation);

        // Assert
        AssertDiagnostics(compilation);
    }

    [Test]
    public void MethodExamplesAreValid([Values] bool allowUnsafe)
    {
        // Arrange
        SyntaxTree[] examples = ForEachType(static (name, source, examples) =>
        {
            if (source.Methods is MethodSource[] methods)
            {
                foreach (MethodSource method in methods)
                {
                    AppendExample($"{name}.{method.Name}", examples, method.Trivia);
                }
            }
        });

        // Act
        AssemblyBuilder.GetGeneratorResult(examples, allowUnsafe, out _, out Compilation compilation);

        // Assert
        AssertDiagnostics(compilation);
    }

    private static SyntaxTree[] ForEachType(Action<string, TypeSource, List<SyntaxTree>> action)
    {
        List<SyntaxTree> examples = new List<SyntaxTree>();

        foreach (KeyValuePair<string, TypeSource> pair in Generator.TypesToGenerate)
        {
            ForEachTypeInternal(examples, pair, action);
        }

        return examples.ToArray();

        static void ForEachTypeInternal(List<SyntaxTree> examples, KeyValuePair<string, TypeSource> pair, Action<string, TypeSource, List<SyntaxTree>> action)
        {
            action(pair.Key, pair.Value, examples);

            if (pair.Value.Types is Dictionary<string, TypeSource> nestedTypes)
            {
                foreach (KeyValuePair<string, TypeSource> nestedPair in nestedTypes)
                {
                    ForEachTypeInternal(examples, nestedPair, action);
                }
            }
        }
    }

    private static void AppendExample(string name, List<SyntaxTree> examples, TriviaSource? trivia)
    {
        if (trivia != null)
        {
            AppendExample(name, examples, trivia.Example);
        }
    }

    private static void AppendExample(string name, List<SyntaxTree> examples, string? example)
    {
        if (string.IsNullOrEmpty(example) || !example.Contains("</code>"))
        {
            return;
        }

        // Insert a random and unique namespace for each example so they don't conflict with other examples
        if (!example.Contains("namespace"))
        {
            StringBuilder sb = new StringBuilder(example);

            int probableStartIndex = example.AsSpan().IndexOfAny(SearchValues.Create(["public ", "internal "], StringComparison.Ordinal));
            if (probableStartIndex == -1)
            {
                Assert.Fail($"Probable start index not found in \n{example}");
                return;
            }

            sb.Insert(probableStartIndex, $"namespace {GetUniqueName()};\n\n");
            example = sb.ToString();
        }

        MatchCollection match = ExampleCodeRegex().Matches(example);
        if (match.Count == 0)
        {
            return;
        }

        for (int i = 0; i < match.Count; i++)
        {
            string exampleName = match.Count > 1 ? $"{name}.{i}" : name;

            examples.Add(AssemblyBuilder.ParseSyntaxTree(match[i].Groups[1].Value.Trim(), exampleName));
        }

        static string GetUniqueName()
        {
            Span<char> result = stackalloc char[32];
            Guid guid = Guid.NewGuid();
            guid.TryFormat(result, out int written, "N");

            result.Replace('0', 'a');
            result.Replace('1', 'b');
            result.Replace('2', 'c');
            result.Replace('3', 'd');
            result.Replace('4', 'e');
            result.Replace('5', 'f');
            result.Replace('6', 'g');
            result.Replace('7', 'h');
            result.Replace('8', 'i');
            result.Replace('9', 'j');

            return new string(result[..written]);
        }
    }

    private static void AssertDiagnostics(Compilation compilation)
    {
        ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics();
        if (diagnostics.Length == 0)
        {
            return;
        }

        diagnostics = diagnostics.Sort((a, b) => string.Compare(a.Location.SourceTree?.FilePath, b.Location.SourceTree?.FilePath, StringComparison.Ordinal));

        using (Assert.EnterMultipleScope())
        {
            for (int i = 0; i < diagnostics.Length; i++)
            {
                Diagnostic d = diagnostics[i];
                switch (d.Severity)
                {
                    case DiagnosticSeverity.Error:
                        Assert.Fail($"{d.Location.SourceTree?.FilePath} :: {d.GetMessage()}\n");
                        break;
                }
            }
        }
    }
}