using Mono.Cecil;

namespace SourceGenUtils.Trimmer;

internal record struct FieldUsage(bool IsUsed, FieldDefinition Field);