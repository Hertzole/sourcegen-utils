namespace Hertzole.SourceGenUtils;

internal abstract class BaseSource
{
    public required string Signature { get; init; }
    public string[]? Dependencies { get; init; }

    /// <summary>
    ///     All these dependencies need to be present in the generated code.
    /// </summary>
    public string[]? RequiredDependencies { get; init; }
}