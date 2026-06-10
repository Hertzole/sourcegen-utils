using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

internal sealed class TypeSource()
{
    public required string Signature { get; init; }
    public required Dictionary<string, MethodSource> Methods { get; init; }
    public Dictionary<string, FieldSource>? Fields { get; init; }
    public Dictionary<string, PropertySource>? Properties { get; init; }

    public bool ContainsMethod(string methodName)
    {
        return Methods.ContainsKey(methodName);
    }
}