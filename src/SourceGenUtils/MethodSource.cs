namespace Hertzole.SourceGenUtils;

internal sealed class MethodSource
{
    public required string Signature { get; init; }
    public required string ReturnStub { get; init; }
    public required string Implementation { get; init; }
    public string[]? Dependencies { get; init; }
}