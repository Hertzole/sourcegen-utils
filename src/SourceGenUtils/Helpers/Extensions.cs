using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Hertzole.SourceGenUtils;

internal static class Extensions
{
    public static StringBuilder AppendLineUnix(this StringBuilder sb, string? value)
    {
        sb.Append(value);
        sb.Append('\n');
        return sb;
    }

    public static StringBuilder AppendLineUnix(this StringBuilder sb)
    {
        sb.Append('\n');
        return sb;
    }

    public static void AddSource(this IncrementalGeneratorPostInitializationContext context, string hintName, CodeWriter writer)
    {
        context.AddSource(hintName, SourceText.From(writer.ToString(), Encoding.UTF8));
        writer.Clear();
    }

    public static void AddSource(this SourceProductionContext context, string hintName, CodeWriter writer)
    {
        context.AddSource(hintName, SourceText.From(writer.ToString(), Encoding.UTF8));
        writer.Clear();
    }

    public static string GetCompleteName(this Type type)
    {
        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(type.Namespace))
        {
            sb.Append(type.Namespace);
            sb.Append('.');
        }

        sb.Append(type.Name);

        if (type.IsGenericType)
        {
            sb.Append("<T>");
        }

        return sb.ToString();
    }
}