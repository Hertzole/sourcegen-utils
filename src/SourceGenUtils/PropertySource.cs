namespace Hertzole.SourceGenUtils;

internal sealed class PropertySource
{
    public required string Signature { get; init; }
    public string[]? Dependencies { get; init; }
}
