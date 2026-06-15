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
        builder.AppendLineUnix("#nullable enable");
        isNullable = true;
    }

    public void AppendConditionalSymbol(string? condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
        {
            return;
        }

        int indent = Indent;
        Indent = 0;
        builder.Append("#if ");
        builder.AppendLineUnix(condition);
        Indent = indent;
    }

    public void AppendPreprocessorSymbol(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        int indent = Indent;
        Indent = 0;

        if (value![0] != '#')
        {
            builder.Append('#');
        }

        builder.AppendLineUnix(value);

        Indent = indent;
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
        builder.AppendLineUnix(namespaceName);
        builder.AppendLineUnix("{");
        Indent++;
        shouldWriteIndent = true;
    }

    public void Append(string value)
    {
        WriteIndentIfNeeded();

        builder.Append(value);
    }

    public void Append(char value)
    {
        WriteIndentIfNeeded();
        builder.Append(value);
    }

    public void AppendLine()
    {
        builder.AppendLineUnix();
        shouldWriteIndent = true;
    }

    public void AppendLine(string value)
    {
        WriteIndentIfNeeded();

        builder.AppendLineUnix(value);
        shouldWriteIndent = true;
    }

    public void Clear()
    {
        builder.Clear();
        Indent = 0;
        shouldWriteIndent = false;
        isNullable = false;
        hasNamespace = false;
        hasWrittenNamespace = false;
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
        if (builder.Length == 0)
        {
            return string.Empty;
        }

        if (hasNamespace && !hasWrittenNamespace)
        {
            Indent--;
            builder.AppendLineUnix("}");

            hasWrittenNamespace = true;
            hasNamespace = false;
        }

        if (isNullable)
        {
            builder.AppendLineUnix("#nullable restore");
        }

        // Trim the last newline, if present.
        if (builder[builder.Length - 1] == '\n')
        {
            builder.Remove(builder.Length - 1, 1);
        }

        return builder.ToString();
    }

    public BlockScope WithBlock()
    {
        return new BlockScope(this);
    }

    public IndentScope WithIndent(int newIndent)
    {
        return new IndentScope(this, newIndent);
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

    internal readonly struct IndentScope : IDisposable
    {
        private readonly CodeWriter writer;
        private readonly int originalIndent;

        public IndentScope(CodeWriter writer, int newIndent)
        {
            this.writer = writer;
            originalIndent = writer.Indent;
            writer.Indent = newIndent;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            writer.Indent = originalIndent;
        }
    }
}