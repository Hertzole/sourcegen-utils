using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private static TypeSource CreateStringBuilderPool()
    {
        const string builder = "System.Text.StringBuilder";
        const string global_builder = $"global::{builder}";
        const string global_pool = $"global::{OBJECT_POOL}<{global_builder}>";

        return new TypeSource
        {
            Signature = "internal static partial class StringBuilderPool",
            Trivia = new TriviaSource
            {
                Summary = "Provides a shared pool of <c>StringBuilder</c> instances to reduce allocations."
            },
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
                    Dependencies = CreatePoolGetDependencies(STRING_BUILDER_POOL),
                    Trivia = new TriviaSource
                    {
                        Summary = "Retrieves a <c>StringBuilder</c> from the pool.",
                        Returns = "A <c>StringBuilder</c> from the pool."
                    }
                },
                new MethodSource
                {
                    Name = "Get",
                    Signature = $"public static partial global::{NAMESPACE}.PoolScope<{global_builder}> Get(out {global_builder} item)",
                    Implementation = (writer, in _) => { writer.AppendLine("return pool.Get(out item);"); },
                    EmptyStub = "item = null!; return default;",
                    Dependencies = CreatePoolGetOutDependencies(STRING_BUILDER_POOL, builder),
                    Trivia = new TriviaSource
                    {
                        Summary = "Retrieves a <c>StringBuilder</c> from the pool and wraps it in a disposable scope that returns it automatically.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["item"] = "When this method returns, contains the <c>StringBuilder</c> retrieved from the pool."
                        },
                        Returns = "A disposable scope that returns the <c>StringBuilder</c> to the pool when disposed."
                    }
                },
                new MethodSource
                {
                    Name = "Return",
                    Signature = $"public static partial void Return({global_builder} item)",
                    Implementation = (writer, in _) => { writer.AppendLine("pool.Return(item);"); },
                    Dependencies = CreatePoolReturnDependencies(STRING_BUILDER_POOL, builder),
                    Trivia = new TriviaSource
                    {
                        Summary = "Returns a <c>StringBuilder</c> to the pool.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["item"] = "The <c>StringBuilder</c> to return to the pool."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "OnCreate",
                    Signature = $"private static {global_builder} OnCreate()",
                    SkipPartial = true,
                    Implementation = (writer, in _) => { writer.AppendLine($"return new {global_builder}(1024);"); },
                    EmptyStub = "return null!;",
                    Trivia = new TriviaSource
                    {
                        Summary = "Creates a new <c>StringBuilder</c> with an initial capacity of 1024 characters.",
                        Returns = "A new <c>StringBuilder</c>."
                    }
                },
                new MethodSource
                {
                    Name = "OnReturn",
                    Signature = $"private static void OnReturn({global_builder} item)",
                    SkipPartial = true,
                    Implementation = (writer, in _) => { writer.AppendLine("item.Clear();"); },
                    Trivia = new TriviaSource
                    {
                        Summary = "Clears the <c>StringBuilder</c> when it is returned to the pool."
                    }
                }
            ]
        };
    }
}