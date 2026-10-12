using Mono.Cecil;

namespace SourceGenUtils.Trimmer;

internal record struct PropertyUsage(bool IsUsed, PropertyDefinition Property);