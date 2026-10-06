using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Hertzole.SourceGen;

[GeneratedCode("Hertzole.SourceGenUtils.Generator", "1.0.0.0")]
[ExcludeFromCodeCoverage]
internal static class StringBuilderPool
{
    private static readonly ObjectPool<StringBuilder> pool = new ObjectPool<StringBuilder>(OnCreate, onReturn: OnReturn);

    public static StringBuilder Get()
    {
        return pool.Get();
    }

    public static void Return(StringBuilder item)
    {
        pool.Return(item);
    }

    private static StringBuilder OnCreate()
    {
        return new StringBuilder(1024);
    }

    private static void OnReturn(StringBuilder item)
    {
        item.Clear();
    }
}