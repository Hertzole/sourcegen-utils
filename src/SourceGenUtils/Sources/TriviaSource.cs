using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hertzole.SourceGenUtils;

[ExcludeFromCodeCoverage]
internal sealed class TriviaSource
{
    public string? Summary { get; init; }
    public string? Returns { get; init; }
    public string? Remarks { get; init; }
    public Dictionary<string, string>? Parameters { get; init; }
}