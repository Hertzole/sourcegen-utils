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

    public void GetMethodDependencies(string methodName, int? paramCount, List<string> deps, CancellationToken cancellationToken)
    {
        if (Methods == null || Methods.Length == 0)
        {
            return;
        }

        foreach (MethodSource m in Methods)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (m.Name == methodName && (!paramCount.HasValue || m.ParameterCount == paramCount.Value) && m.Dependencies != null)
            {
                deps.AddRange(m.Dependencies);
            }
        }
    }

    public void GetMethodDependencies(string methodName, string parameterTypesKey, List<string> deps, CancellationToken cancellationToken)
    {
        if (Methods == null || Methods.Length == 0)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        foreach (MethodSource m in Methods)
        {
            if (m.Name == methodName && m.ParameterTypesKey == parameterTypesKey && m.Dependencies != null)
            {
                deps.AddRange(m.Dependencies);
                return;
            }
        }
    }

    public void GetMethodDependenciesRecursive(string methodPath, string parameterTypesKey, List<string> deps, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int dot = methodPath.IndexOf('.');
        if (dot < 0)
        {
            GetMethodDependencies(methodPath, parameterTypesKey, deps, cancellationToken);
            return;
        }

        string nestedName = methodPath.Substring(0, dot);
        string rest = methodPath.Substring(dot + 1);

        if (Types != null && Types.TryGetValue(nestedName, out TypeSource? nested))
        {
            nested.GetMethodDependenciesRecursive(rest, parameterTypesKey, deps, cancellationToken);
        }
    }

    public void GetMethodDependenciesRecursive(string methodPath, int? paramCount, List<string> deps, CancellationToken cancellationToken)
    {
        int dot = methodPath.IndexOf('.');
        if (dot < 0)
        {
            GetMethodDependencies(methodPath, paramCount, deps, cancellationToken);
            return;
        }

        string nestedName = methodPath.Substring(0, dot);
        string rest = methodPath.Substring(dot + 1);

        if (Types != null && Types.TryGetValue(nestedName, out TypeSource? nested))
        {
            nested.GetMethodDependenciesRecursive(rest, paramCount, deps, cancellationToken);
        }
    }
}