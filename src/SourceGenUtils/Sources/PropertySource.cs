namespace Hertzole.SourceGenUtils;

internal sealed class PropertySource : BaseSource
{
    public string[]? GetAttributes { get; init; }
    public string[]? SetAttributes { get; init; }
    public ImplementationHandler? GetImplementation { get; init; }
    public ImplementationHandler? SetImplementation { get; init; }
}