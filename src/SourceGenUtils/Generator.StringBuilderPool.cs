using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private static TypeSource CreateStringBuilderPool()
    {
        const string builder = "System.Text.StringBuilder";
        const string global_builder = $"global::{builder}";
        const string global_pool = $"global::{OBJECT_POOL}<{global_builder}>";
        const string builder_pool = $"{NAMESPACE}.StringBuilderPool";

        return new TypeSource
        {
            Signature = "internal static partial class StringBuilderPool",
            Fields = new Dictionary<string, FieldSource>
            {
                ["pool"] = new FieldSource
                {
                    Signature = $"private static readonly {global_pool} pool = new {global_pool}(OnCreate, onReturn: OnReturn);"
                }
            },
            Methods =
            [
                new MethodSource
                {
                    Name = "Get",
                    Signature = $"public static partial {global_builder} Get()",
                    Implementation = (writer, in _) => { writer.AppendLine("return pool.Get();"); },
                    EmptyStub = "return null!;",
                    Dependencies = CreatePoolGetDependencies(builder_pool)
                },
                new MethodSource
                {
                    Name = "Get",
                    Signature = $"public static partial global::{NAMESPACE}.PoolScope<{global_builder}> Get(out {global_builder} item)",
                    Implementation = (writer, in _) => { writer.AppendLine("return pool.Get(out item);"); },
                    EmptyStub = "item = null!; return default;",
                    Dependencies = CreatePoolGetOutDependencies(builder_pool, builder)
                },
                new MethodSource
                {
                    Name = "Return",
                    Signature = $"public static partial void Return({global_builder} item)",
                    Implementation = (writer, in _) => { writer.AppendLine("pool.Return(item);"); },
                    Dependencies = CreatePoolReturnDependencies(builder_pool, builder)
                },
                new MethodSource
                {
                    Name = "OnCreate",
                    Signature = $"private static {global_builder} OnCreate()",
                    SkipPartial = true,
                    Implementation = (writer, in _) => { writer.AppendLine($"return new {global_builder}(1024);"); },
                    EmptyStub = "return null!;"
                },
                new MethodSource
                {
                    Name = "OnReturn",
                    Signature = $"private static void OnReturn({global_builder} item)",
                    SkipPartial = true,
                    Implementation = (writer, in _) => { writer.AppendLine("item.Clear();"); }
                }
            ]
        };
    }
}