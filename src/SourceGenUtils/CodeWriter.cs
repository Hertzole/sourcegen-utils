using System;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Hertzole.SourceGenUtils;

internal sealed class CodeWriter
{
    private readonly StringBuilder sb = new StringBuilder(1024);

    private bool shouldWriteIndent = false;
    private bool hasNamespace = false;
    private bool hasWrittenNamespace = false;
    private bool isNullable = false;

    public int Indent { get; set; }

    public void AppendNullable()
    {
        sb.AppendLine("#nullable enable");
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
        sb.Append("namespace ");
        sb.AppendLine(namespaceName);
        sb.AppendLine("{");
        Indent++;
        shouldWriteIndent = true;
    }

    public void Append(string value)
    {
        WriteIndentIfNeeded();

        sb.Append(value);
    }

    public void AppendLine()
    {
        sb.AppendLine();
        shouldWriteIndent = true;
    }

    public void AppendLine(string value)
    {
        WriteIndentIfNeeded();

        sb.AppendLine(value);
        shouldWriteIndent = true;
    }

    private void WriteIndentIfNeeded()
    {
        if (!shouldWriteIndent)
        {
            return;
        }

        shouldWriteIndent = false;
        sb.Append(' ', Indent * 4);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (hasNamespace && !hasWrittenNamespace)
        {
            Indent--;
            sb.AppendLine("}");

            hasWrittenNamespace = true;
            hasNamespace = false;
        }

        if (isNullable)
        {
            sb.AppendLine("#nullable restore");
        }

        return sb.ToString();
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