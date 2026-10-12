using Mono.Cecil;

namespace SourceGenUtils.Trimmer;

internal static class CecilExtensions
{
    private static readonly object resolveLock = new object();

    public static TypeDefinition LockedResolve(this TypeReference reference)
    {
        lock (resolveLock)
        {
            return reference.Resolve();
        }
    }

    public static MethodDefinition LockedResolve(this MethodReference reference)
    {
        lock (resolveLock)
        {
            return reference.Resolve();
        }
    }
}