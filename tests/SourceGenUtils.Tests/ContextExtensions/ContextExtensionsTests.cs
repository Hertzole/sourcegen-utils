using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Hertzole.SourceGen.Wrappers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

public class ContextExtensionsTests : GeneratorTests
{
    private const string HINT = "TestHint";
    private const string CONTENT = "Hello";

    private sealed class CaptureIncremental : IIncrementalGenerator
    {
        private readonly Action<SourceProductionContext>? sourceOutput;
        private readonly Action<IncrementalGeneratorPostInitializationContext>? postInitializationOutput;

        public CaptureIncremental(Action<SourceProductionContext>? sourceOutput = null,
            Action<IncrementalGeneratorPostInitializationContext>? postInitializationOutput = null)
        {
            this.sourceOutput = sourceOutput;
            this.postInitializationOutput = postInitializationOutput;
        }

        /// <inheritdoc />
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            if (postInitializationOutput != null)
            {
                context.RegisterPostInitializationOutput(ctx => postInitializationOutput(ctx));
            }

            if (sourceOutput != null)
            {
                context.RegisterSourceOutput(context.CompilationProvider, (ctx, _) => sourceOutput(ctx));
            }
        }
    }

#pragma warning disable RS1042
    private sealed class CaptureLegacy : ISourceGenerator
    {
        private readonly Action<GeneratorExecutionContext>? execute;
        private readonly Action<GeneratorPostInitializationContext>? postInitialization;

        public CaptureLegacy(Action<GeneratorExecutionContext>? execute = null,
            Action<GeneratorPostInitializationContext>? postInitialization = null)
        {
            this.execute = execute;
            this.postInitialization = postInitialization;
        }

        /// <inheritdoc />
        public void Initialize(GeneratorInitializationContext context)
        {
            if (postInitialization != null)
            {
                context.RegisterForPostInitialization(ctx => postInitialization(ctx));
            }
        }

        /// <inheritdoc />
        public void Execute(GeneratorExecutionContext context)
        {
            execute?.Invoke(context);
        }
    }
#pragma warning restore RS1042

    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "ContextExtensions";
    }

    [Test]
    public void AddSource_SourceProductionContext()
    {
        // Arrange
        (ContextExtensions extensions, CodeWriter writer) = CompileContext(Usage("SourceProductionContext", false));
        writer.Append(CONTENT);

        // Act
        GeneratorDriverRunResult result = RunIncremental(ctx => extensions.AddSource(ctx, HINT, writer));

        // Assert
        AssertAddSource(result, writer, Encoding.UTF8);
    }

    [Test]
    public void AddSource_SourceProductionContext_WithEncoding()
    {
        // Arrange
        (ContextExtensions extensions, CodeWriter writer) = CompileContext(Usage("SourceProductionContext", true));
        writer.Append(CONTENT);

        // Act
        GeneratorDriverRunResult result = RunIncremental(ctx => extensions.AddSource(ctx, HINT, writer, Encoding.UTF32));

        // Assert
        AssertAddSource(result, writer, Encoding.UTF32);
    }

    [Test]
    public void AddSource_IncrementalGeneratorPostInitializationContext()
    {
        // Arrange
        (ContextExtensions extensions, CodeWriter writer) =
            CompileContext(Usage("IncrementalGeneratorPostInitializationContext", false));

        writer.Append(CONTENT);

        // Act
        GeneratorDriverRunResult result = RunIncremental(postInit: ctx => extensions.AddSource(ctx, HINT, writer));

        // Assert
        AssertAddSource(result, writer, Encoding.UTF8);
    }

    [Test]
    public void AddSource_IncrementalGeneratorPostInitializationContext_WithEncoding()
    {
        // Arrange
        (ContextExtensions extensions, CodeWriter writer) =
            CompileContext(Usage("IncrementalGeneratorPostInitializationContext", true));

        writer.Append(CONTENT);

        // Act
        GeneratorDriverRunResult result =
            RunIncremental(postInit: ctx => extensions.AddSource(ctx, HINT, writer, Encoding.UTF32));

        // Assert
        AssertAddSource(result, writer, Encoding.UTF32);
    }

    [Test]
    public void AddSource_GeneratorExecutionContext()
    {
        // Arrange
        (ContextExtensions extensions, CodeWriter writer) = CompileContext(Usage("GeneratorExecutionContext", false));
        writer.Append(CONTENT);

        // Act
        GeneratorDriverRunResult result = RunLegacy(ctx => extensions.AddSource(ctx, HINT, writer));

        // Assert
        AssertAddSource(result, writer, Encoding.UTF8);
    }

    [Test]
    public void AddSource_GeneratorExecutionContext_WithEncoding()
    {
        // Arrange
        (ContextExtensions extensions, CodeWriter writer) = CompileContext(Usage("GeneratorExecutionContext", true));
        writer.Append(CONTENT);

        // Act
        GeneratorDriverRunResult result = RunLegacy(ctx => extensions.AddSource(ctx, HINT, writer, Encoding.UTF32));

        // Assert
        AssertAddSource(result, writer, Encoding.UTF32);
    }

    [Test]
    public void AddSource_GeneratorPostInitializationContext()
    {
        // Arrange
        (ContextExtensions extensions, CodeWriter writer) =
            CompileContext(Usage("GeneratorPostInitializationContext", false));

        writer.Append(CONTENT);

        // Act
        GeneratorDriverRunResult result = RunLegacy(postInit: ctx => extensions.AddSource(ctx, HINT, writer));

        // Assert
        AssertAddSource(result, writer, Encoding.UTF8);
    }

    [Test]
    public void AddSource_GeneratorPostInitializationContext_WithEncoding()
    {
        // Arrange
        (ContextExtensions extensions, CodeWriter writer) =
            CompileContext(Usage("GeneratorPostInitializationContext", true));

        writer.Append(CONTENT);

        // Act
        GeneratorDriverRunResult result = RunLegacy(postInit: ctx => extensions.AddSource(ctx, HINT, writer, Encoding.UTF32));

        // Assert
        AssertAddSource(result, writer, Encoding.UTF32);
    }

    private static string Usage(string contextType, bool withEncoding)
    {
        string encoding = withEncoding ? ", global::System.Text.Encoding.UTF8" : string.Empty;

        return $"var writer = new CodeWriter(); writer.Append(\"{CONTENT}\"); " +
               $"_ = writer.ToString(); " +
               $"ContextExtensions.AddSource(default(global::Microsoft.CodeAnalysis.{contextType}), \"{HINT}\", writer{encoding});";
    }

    private static (ContextExtensions Extensions, CodeWriter Writer) CompileContext(string usage)
    {
        Assembly assembly = AssemblyBuilder.CompileAssemblyByUsing(usage);
        Type extensions = assembly.GetType($"{NAMESPACE}.ContextExtensions", true)!;
        Type writer = assembly.GetType($"{NAMESPACE}.CodeWriter", true)!;

        return (new ContextExtensions(extensions), new CodeWriter(writer));
    }

    private static GeneratorDriverRunResult RunIncremental(Action<SourceProductionContext>? source = null,
        Action<IncrementalGeneratorPostInitializationContext>? postInit = null)
    {
        IIncrementalGenerator generator = new CaptureIncremental(source, postInit);
        return CSharpGeneratorDriver.Create(generator.AsSourceGenerator())
                                    .RunGenerators(GetTrivialCompilation())
                                    .GetRunResult();
    }

    private static GeneratorDriverRunResult RunLegacy(Action<GeneratorExecutionContext>? execute = null,
        Action<GeneratorPostInitializationContext>? postInit = null)
    {
        ISourceGenerator generator = new CaptureLegacy(execute, postInit);
        return CSharpGeneratorDriver.Create(generator)
                                    .RunGenerators(GetTrivialCompilation())
                                    .GetRunResult();
    }

    private static CSharpCompilation GetTrivialCompilation()
    {
        return CSharpCompilation.Create("ContextExtensionsTests", Array.Empty<SyntaxTree>(),
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
    }

    private static void AssertAddSource(GeneratorDriverRunResult result, CodeWriter writer, Encoding encoding)
    {
        SyntaxTree? tree = result.GeneratedTrees.SingleOrDefault(t => t.FilePath.EndsWith($"{HINT}.cs", StringComparison.Ordinal));

        Assert.That(tree, Is.Not.Null, $"No generated source found for hint '{HINT}'.");
        Assert.That(tree!.ToString(), Is.EqualTo(CONTENT));
        Assert.That(tree.GetText().Encoding?.WebName, Is.EqualTo(encoding.WebName));
        Assert.That(writer.ToString(), Is.Empty, "The writer should have been cleared.");
    }
}