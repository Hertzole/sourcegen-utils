using System;
using System.Collections.Generic;
using System.Threading;

namespace Hertzole.SourceGenUtils;

internal readonly struct ImplementationContext
{
    private readonly HashSet<string> calledMethods;
    private readonly HashSet<string> calledMethodsWithoutArgs;

    public readonly CancellationToken CancellationToken;
    public readonly bool AllowUnsafe;

    public ImplementationContext(HashSet<string> calledMethods, CancellationToken cancellationToken, bool allowUnsafe)
    {
        this.calledMethods = calledMethods;
        calledMethodsWithoutArgs = new HashSet<string>(calledMethods, OnlyMethodNameEquality.Instance);
        CancellationToken = cancellationToken;
        AllowUnsafe = allowUnsafe;
    }

    public bool HasCalledMethod(string method)
    {
        // Normalize: strip '?' from parameter types for nullable matching.
        // The calledMethods set stores keys without '?', so if the lookup key
        // somehow has one, remove it before searching.
        ReadOnlySpan<char> span = method.AsSpan();
        int startPos = span.IndexOf('(');
        int endPos = span.IndexOf(')');
        if (startPos == -1 && endPos == -1)
        {
            // No args, use other
            return calledMethodsWithoutArgs.Contains(method);
        }

        if (endPos > startPos)
        {
            // Check if there's a '?' in the parameter portion that doesn't belong
            ReadOnlySpan<char> paramsPortion = span.Slice(startPos + 1, endPos - startPos - 1);
            if (paramsPortion.IndexOf('?') >= 0)
            {
                method = method.Replace("?", string.Empty);
            }
        }

        return calledMethods.Contains(method);
    }
}