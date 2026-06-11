using System;
using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

internal sealed class MethodSource
{
    public required string Name { get; init; }
    public required string Signature { get; init; }
    public string EmptyStub { get; init; } = string.Empty;
    public required Action<CodeWriter> Implementation { get; init; }
    public string[]? Dependencies { get; init; }
    public Guid Identifier { get; } = Guid.NewGuid();
    public bool SkipPartial { get; init; } = false;

    private int? parameterCount;
    private string? parameterTypesKey;

    public int ParameterCount
    {
        get
        {
            if (parameterCount.HasValue)
            {
                return parameterCount.Value;
            }

            int openParen = Signature.IndexOf('(');
            int closeParen = Signature.LastIndexOf(')');
            if (openParen < 0 || closeParen <= openParen)
            {
                parameterCount = -1;
                return -1;
            }

            string inner = Signature.Substring(openParen + 1, closeParen - openParen - 1).Trim();
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

            int openParen = Signature.IndexOf('(');
            int closeParen = Signature.LastIndexOf(')');
            if (openParen < 0 || closeParen <= openParen + 1)
            {
                parameterTypesKey = string.Empty;
                Log.Info(Name + " Empty 1");
                return string.Empty;
            }

            string inner = Signature.Substring(openParen + 1, closeParen - openParen - 1).Trim();
            if (inner.Length == 0)
            {
                parameterTypesKey = string.Empty;
                Log.Info(Name + " Empty 2");
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
                    types.Add(ExtractTypeName(inner.Substring(start, i - start)));
                    start = i + 1;
                }
            }

            types.Add(ExtractTypeName(inner.Substring(start)));

            parameterTypesKey = string.Join(",", types);
            Log.Info<MethodSource>($"{Name}: {parameterTypesKey}");
            return parameterTypesKey;
        }
    }

    private static string ExtractTypeName(string paramSegment)
    {
        // "string value" → "string"
        // "global::Microsoft.CodeAnalysis.INamespaceSymbol? symbol" → "Microsoft.CodeAnalysis.INamespaceSymbol?"
        int lastSpace = paramSegment.LastIndexOf(' ');
        string typePart;
        if (lastSpace >= 0)
        {
            typePart = paramSegment.Substring(0, lastSpace).Trim();
        }
        else
        {
            typePart = paramSegment.Trim();
        }

        if (typePart.StartsWith("global::", StringComparison.Ordinal))
        {
            typePart = typePart.Substring(8);
        }

        return typePart;
    }
}