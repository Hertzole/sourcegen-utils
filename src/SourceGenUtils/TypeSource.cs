using System;
using System.Collections.Generic;
using System.Threading;

namespace Hertzole.SourceGenUtils;

internal sealed class TypeSource : IHasAttributes
{
    public required string Signature { get; init; }
    public MethodSource[]? Methods { get; init; }
    public Dictionary<string, FieldSource>? Fields { get; init; }
    public Dictionary<string, PropertySource>? Properties { get; init; }
    public Dictionary<string, TypeSource>? Types { get; init; }
    public string[]? Attributes { get; init; }
    public string? ConditionalPreprocessorSymbol { get; init; }
    public TriviaSource? Trivia { get; init; }

    public bool ContainsMethod(string methodName, CancellationToken cancellationToken)
    {
        if (Methods == null || Methods.Length == 0)
        {
            return false;
        }

        foreach (MethodSource m in Methods)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (m.Name == methodName)
            {
                return true;
            }
        }

        if (Types != null)
        {
            foreach (KeyValuePair<string, TypeSource> kvp in Types)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (kvp.Value.ContainsMethod(methodName, cancellationToken))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public string[]? GetMethodDependencies(string methodName, int? paramCount, CancellationToken cancellationToken)
    {
        if (Methods == null || Methods.Length == 0)
        {
            return Array.Empty<string>();
        }

        List<string>? deps = null;
        foreach (MethodSource m in Methods)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (m.Name == methodName
                && (!paramCount.HasValue || m.ParameterCount == paramCount.Value)
                && m.Dependencies != null)
            {
                if (deps == null)
                {
                    deps = new List<string>(m.Dependencies);
                }
                else
                {
                    deps.AddRange(m.Dependencies);
                }
            }
        }

        return deps?.ToArray();
    }

    public string[]? GetMethodDependencies(string methodName, string parameterTypesKey, CancellationToken cancellationToken)
    {
        if (Methods == null || Methods.Length == 0)
        {
            return Array.Empty<string>();
        }

        cancellationToken.ThrowIfCancellationRequested();

        foreach (MethodSource m in Methods)
        {
            if (m.Name == methodName && m.ParameterTypesKey == parameterTypesKey)
            {
                return m.Dependencies;
            }
        }

        return null;
    }

    public string[]? GetMethodDependenciesRecursive(string methodPath, string parameterTypesKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int dot = methodPath.IndexOf('.');
        if (dot < 0)
        {
            return GetMethodDependencies(methodPath, parameterTypesKey, cancellationToken);
        }

        string nestedName = methodPath.Substring(0, dot);
        string rest = methodPath.Substring(dot + 1);

        if (Types != null && Types.TryGetValue(nestedName, out TypeSource? nested))
        {
            return nested.GetMethodDependenciesRecursive(rest, parameterTypesKey, cancellationToken);
        }

        return null;
    }

    public string[]? GetMethodDependenciesRecursive(string methodPath, int? paramCount, CancellationToken cancellationToken)
    {
        int dot = methodPath.IndexOf('.');
        if (dot < 0)
        {
            return GetMethodDependencies(methodPath, paramCount, cancellationToken);
        }

        string nestedName = methodPath.Substring(0, dot);
        string rest = methodPath.Substring(dot + 1);

        if (Types != null && Types.TryGetValue(nestedName, out TypeSource? nested))
        {
            return nested.GetMethodDependenciesRecursive(rest, paramCount, cancellationToken);
        }

        return null;
    }
}