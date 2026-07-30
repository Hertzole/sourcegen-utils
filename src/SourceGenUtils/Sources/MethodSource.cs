using System;
using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

internal sealed class MethodSource : BaseSource
{
    private int? parameterCount;
    private string? parameterTypesKey;

    public required string Name { get; init; }
    public string EmptyStub { get; init; } = string.Empty;
    public required ImplementationHandler Implementation { get; init; }
    public bool SkipPartial { get; init; } = false;
    public bool AlwaysWrite { get; init; }

    public int ParameterCount
    {
        get
        {
            if (parameterCount.HasValue)
            {
                return parameterCount.Value;
            }

            ReadOnlySpan<char> signatureSpan = Signature.AsSpan();

            int openParen = signatureSpan.IndexOf('(');
            int closeParen = signatureSpan.LastIndexOf(')');
            if (openParen < 0 || closeParen <= openParen)
            {
                parameterCount = -1;
                return -1;
            }

            ReadOnlySpan<char> inner = signatureSpan.Slice(openParen + 1, closeParen - openParen - 1).Trim();
            if (inner.Length == 0)
            {
                parameterCount = 0;
                return 0;
            }

            int count = 1;
            int depth = 0;
            foreach (char c in inner)
            {
                if (c == '(' || c == '<' || c == '[')
                {
                    depth++;
                }
                else if (c == ')' || c == '>' || c == ']')
                {
                    depth--;
                }
                else if (c == ',' && depth == 0)
                {
                    count++;
                }
            }

            parameterCount = count;
            return count;
        }
    }

    public string ParameterTypesKey
    {
        get
        {
            if (parameterTypesKey != null)
            {
                return parameterTypesKey;
            }

            ReadOnlySpan<char> signatureSpan = Signature.AsSpan();

            int openParen = signatureSpan.IndexOf('(');
            int closeParen = signatureSpan.LastIndexOf(')');
            if (openParen < 0 || closeParen <= openParen + 1)
            {
                parameterTypesKey = string.Empty;
                return string.Empty;
            }

            ReadOnlySpan<char> inner = signatureSpan.Slice(openParen + 1, closeParen - openParen - 1).Trim();
            if (inner.Length == 0)
            {
                parameterTypesKey = string.Empty;
                return string.Empty;
            }

            List<string> types = new List<string>();
            int depth = 0;
            int start = 0;
            for (int i = 0; i < inner.Length; i++)
            {
                char c = inner[i];
                if (c == '(' || c == '<' || c == '[')
                {
                    depth++;
                }
                else if (c == ')' || c == '>' || c == ']')
                {
                    depth--;
                }
                else if (c == ',' && depth == 0)
                {
                    types.Add(ExtractTypeName(inner.Slice(start, i - start)));
                    start = i + 1;
                }
            }

            types.Add(ExtractTypeName(inner.Slice(start)));

            parameterTypesKey = string.Join(", ", types);
            return parameterTypesKey;
        }
    }

    private static string ExtractTypeName(ReadOnlySpan<char> paramSegment)
    {
        // Strip default value (e.g. " = default")
        int equalsIndex = paramSegment.IndexOf('=');
        if (equalsIndex >= 0)
        {
            paramSegment = paramSegment.Slice(0, equalsIndex).TrimEnd();
        }

        // Extract type part: everything before the last space (parameter name)
        int lastSpace = paramSegment.LastIndexOf(' ');
        ReadOnlySpan<char> typePart;
        if (lastSpace >= 0)
        {
            typePart = paramSegment.Slice(0, lastSpace).Trim();
        }
        else
        {
            typePart = paramSegment.Trim();
        }

        // Strip parameter modifiers
        if (typePart.StartsWith("this ", StringComparison.Ordinal))
        {
            typePart = typePart.Slice(5);
        }
        else if (typePart.StartsWith("out ", StringComparison.Ordinal) || typePart.StartsWith("ref ", StringComparison.Ordinal))
        {
            typePart = typePart.Slice(4);
        }
        else if (typePart.StartsWith("in ", StringComparison.Ordinal))
        {
            typePart = typePart.Slice(3);
        }
        else if (typePart.StartsWith("params ", StringComparison.Ordinal))
        {
            typePart = typePart.Slice(7);
        }

        if (typePart.StartsWith("global::", StringComparison.Ordinal))
        {
            typePart = typePart.Slice(8);
        }

        if (typePart.EndsWith("?"))
        {
            typePart = typePart.Slice(0, typePart.Length - 1);
        }

        return typePart.ToString();
    }
}