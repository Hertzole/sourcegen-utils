using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

internal sealed class TriviaSource
{
    public string? Summary { get; init; }
    public string? Returns { get; init; }
    public Dictionary<string, string>? Parameters { get; init; }
}