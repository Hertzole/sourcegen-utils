using Mono.Cecil;

namespace SourceGenUtils.Trimmer;

internal record struct MethodUsage(bool IsUsed, MethodDefinition Method);