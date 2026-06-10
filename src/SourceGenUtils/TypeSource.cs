using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

internal sealed class TypeSource()
{
    public required string Signature { get; init; }
    public required MethodSource[] Methods { get; init; }
    public Dictionary<string, FieldSource>? Fields { get; init; }
    public Dictionary<string, PropertySource>? Properties { get; init; }
    public Dictionary<string, TypeSource>? Types { get; init; }

    public bool ContainsMethod(string methodName)
    {
        foreach (MethodSource m in Methods)
        {
            if (m.Name == methodName)
            {
                return true;
            }
        }

        if (Types != null)
        {
            foreach (KeyValuePair<string, TypeSource> kvp in Types)
            {
                if (kvp.Value.ContainsMethod(methodName))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public string[]? GetMethodDependencies(string methodName, int? paramCount = null)
    {
        List<string>? deps = null;
        foreach (MethodSource m in Methods)
        {
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

    public string[]? GetMethodDependencies(string methodName, string parameterTypesKey)
    {
        foreach (MethodSource m in Methods)
        {
            if (m.Name == methodName && m.ParameterTypesKey == parameterTypesKey)
            {
                return m.Dependencies;
            }
        }

        return null;
    }

    public string[]? GetMethodDependenciesRecursive(string methodPath, string parameterTypesKey)
    {
        int dot = methodPath.IndexOf('.');
        if (dot < 0)
        {
            return GetMethodDependencies(methodPath, parameterTypesKey);
        }

        string nestedName = methodPath.Substring(0, dot);
        string rest = methodPath.Substring(dot + 1);

        if (Types != null && Types.TryGetValue(nestedName, out TypeSource? nested))
        {
            return nested.GetMethodDependenciesRecursive(rest, parameterTypesKey);
        }

        return null;
    }

    public string[]? GetMethodDependenciesRecursive(string methodPath, int? paramCount = null)
    {
        int dot = methodPath.IndexOf('.');
        if (dot < 0)
        {
            return GetMethodDependencies(methodPath, paramCount);
        }

        string nestedName = methodPath.Substring(0, dot);
        string rest = methodPath.Substring(dot + 1);

        if (Types != null && Types.TryGetValue(nestedName, out TypeSource? nested))
        {
            return nested.GetMethodDependenciesRecursive(rest, paramCount);
        }

        return null;
    }
}