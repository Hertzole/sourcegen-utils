namespace Hertzole.SourceGenUtils;

internal static class Constants
{
    public const string NAMESPACE = Generator.NAMESPACE;

    public const string CODE_WRITER = $"{NAMESPACE}.CodeWriter";
    public const string GLOBAL_CODE_WRITER = $"global::{CODE_WRITER}";

    public const string ARRAY_BUILDER = $"{NAMESPACE}.ArrayBuilder";
    public const string GLOBAL_ARRAY_BUILDER = $"global::{ARRAY_BUILDER}";

    public const string STRING_BUILDER_POOL = $"{NAMESPACE}.StringBuilderPool";
    public const string GLOBAL_STRING_BUILDER_POOL = $"global::{STRING_BUILDER_POOL}";

    public const string MS_CODE = "Microsoft.CodeAnalysis";
    public const string GLOBAL_MS_CODE = $"global::{MS_CODE}";

    public const string R_SPAN = "System.ReadOnlySpan";
    public const string GLOBAL_R_SPAN = $"global::{R_SPAN}";

    public const string MEMORY_EXT = "System.MemoryExtensions";
    public const string GLOBAL_MEMORY_EXT = $"global::{MEMORY_EXT}";

    public static readonly string[] AggressiveInlineAttribute =
        ["global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)"];
}