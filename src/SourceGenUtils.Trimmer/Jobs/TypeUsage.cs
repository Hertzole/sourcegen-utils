using Mono.Cecil;

namespace SourceGenUtils.Trimmer;

internal record struct TypeUsage(bool IsUsed, TypeDefinition Type);