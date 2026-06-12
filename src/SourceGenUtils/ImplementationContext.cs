using System;
using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

internal readonly struct ImplementationContext
{
    private readonly HashSet<string> calledMethods;
    private readonly HashSet<string> calledMethodsWithoutArgs;

    public ImplementationContext(HashSet<string> calledMethods)
    {
        this.calledMethods = calledMethods;
        calledMethodsWithoutArgs = new HashSet<string>(calledMethods, OnlyMethodNameEquality.Instance);
    }

    public bool HasCalledMethod(string method)
    {
        ReadOnlySpan<char> span = method.AsSpan();
        int startPos = span.IndexOf('(');
        int endPos = span.IndexOf(')');
        if (startPos == -1 && endPos == -1)
        {
            // No args, use other 
            return calledMethodsWithoutArgs.Contains(method);
        }

        return calledMethods.Contains(method);
    }
}