using System;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Hertzole.SourceGenUtils;

internal sealed class CodeWriter
{
    private readonly StringBuilder builder = new StringBuilder(1024);

    private bool shouldWriteIndent = false;
    private bool hasNamespace = false;
    private bool hasWrittenNamespace = false;
    private bool isNullable = false;

    public int Indent { get; set; }

    public void AppendNullable()
    {
        builder.AppendLine("#nullable enable");
        isNullable = true;
    }

    public void AppendNamespace(INamespaceSymbol? namespaceSymbol)
    {
        if (namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace)
        {
            return;
        }

        if (hasNamespace)
        {
            return;
        }

        AppendNamespace(namespaceSymbol.ToDisplayString());
    }

    public void AppendNamespace(string namespaceName)
    {
        if (string.IsNullOrWhiteSpace(namespaceName))
        {
            return;
        }

        hasNamespace = true;
        hasWrittenNamespace = false;
        builder.Append("namespace ");
        builder.AppendLine(namespaceName);
        builder.AppendLine("{");
        Indent++;
        shouldWriteIndent = true;
    }

    public void Append(string value)
    {
        WriteIndentIfNeeded();

        builder.Append(value);
    }

    public void AppendLine()
    {
        builder.AppendLine();
        shouldWriteIndent = true;
    }

    public void AppendLine(string value)
    {
        WriteIndentIfNeeded();

        builder.AppendLine(value);
        shouldWriteIndent = true;
    }

    public void AppendGeneratedCodeAttribute(string generator, string version)
    {
        WriteIndentIfNeeded();
        builder.Append("[global::System.CodeDom.Compiler.GeneratedCode(\"");
        builder.Append(generator);
        builder.Append("\", \"");
        builder.Append(version);
        builder.Append("\")]\n");
        shouldWriteIndent = true;
    }

    public void AppendExcludeFromCodeCoverageAttribute()
    {
        WriteIndentIfNeeded();
        builder.Append("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]\n");
        shouldWriteIndent = true;
    }

    private void WriteIndentIfNeeded()
    {
        if (!shouldWriteIndent)
        {
            return;
        }

        shouldWriteIndent = false;
        builder.Append(' ', Indent * 4);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (hasNamespace && !hasWrittenNamespace)
        {
            Indent--;
            builder.AppendLine("}");

            hasWrittenNamespace = true;
            hasNamespace = false;
        }

        if (isNullable)
        {
            builder.AppendLine("#nullable restore");
        }

        return builder.ToString();
    }

    public BlockScope WithBlock()
    {
        return new BlockScope(this);
    }

    internal readonly struct BlockScope : IDisposable
    {
        private readonly CodeWriter writer;

        public BlockScope(CodeWriter writer)
        {
            this.writer = writer;
            writer.AppendLine("{");
            writer.Indent++;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            writer.Indent--;
            writer.AppendLine("}");
        }
    }
}