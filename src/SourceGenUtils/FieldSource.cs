namespace Hertzole.SourceGenUtils;

internal sealed class FieldSource
{
    public required string Signature { get; init; }
    public string[]? Dependencies { get; init; }
}
