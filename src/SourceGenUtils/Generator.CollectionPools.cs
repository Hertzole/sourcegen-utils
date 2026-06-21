using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private static TypeSource CreateListPool()
    {
        return CreateCollectionPool("ListPool", "System.Collections.Generic.List<T>");
    }

    private static TypeSource CreateHashSetPool()
    {
        return CreateCollectionPool("HashSetPool", "System.Collections.Generic.HashSet<T>");
    }

    private static TypeSource CreateStackPool()
    {
        return CreateCollectionPool("StackPool", "System.Collections.Generic.Stack<T>");
    }

    private static TypeSource CreateQueuePool()
    {
        return CreateCollectionPool("QueuePool", "System.Collections.Generic.Queue<T>");
    }

    private static TypeSource CreateCollectionPool(string name, string collection)
    {
        string globalCollection = $"global::{collection}";
        string pool = $"global::{NAMESPACE}.ObjectPool<{globalCollection}>";
        string collectionPool = $"{NAMESPACE}.{name}";

        return new TypeSource
        {
            Signature = $"internal static partial class {name}<T>",
            Fields = new Dictionary<string, FieldSource>
            {
                ["pool"] = new FieldSource
                {
                    Signature = $"private static {pool} pool = new {pool}(OnCreate, onReturn: OnReturn);",
                    Dependencies = [$"{collectionPool}.Get", $"{collectionPool}.Return"]
                }
            },
            Methods =
            [
                new MethodSource
                {
                    Name = "Get",
                    Signature = $"public static partial {globalCollection} Get()",
                    Implementation = (writer, in context) => { writer.AppendLine("return pool.Get();"); },
                    EmptyStub = "return null!;",
                    Dependencies =
                    [
                        $"{collectionPool}.OnCreate()",
                        $"{OBJECT_POOL}.ObjectPool(System.Func<T>, System.Action<T>?, System.Action<T>?, System.Action<T>?)",
                        $"{OBJECT_POOL}.Get()"
                    ]
                },
                new MethodSource
                {
                    Name = "Get",
                    Signature = $"public static partial global::{NAMESPACE}.PoolScope<{globalCollection}> Get(out {globalCollection} item)",
                    Implementation = (writer, in context) => { writer.AppendLine("return pool.Get(out item);"); },
                    EmptyStub = "item = null!; return default;",
                    Dependencies =
                    [
                        $"{collectionPool}.OnCreate()",
                        $"{collectionPool}.OnReturn({collection})",
                        $"{OBJECT_POOL}.ObjectPool(System.Func<T>, System.Action<T>?, System.Action<T>?, System.Action<T>?)",
                        $"{OBJECT_POOL}.Get(T)"
                    ]
                },
                new MethodSource
                {
                    Name = "Return",
                    Signature = $"public static partial void Return({globalCollection} item)",
                    Implementation = (writer, in context) => { writer.AppendLine("pool.Return(item);"); },
                    Dependencies =
                    [
                        $"{collectionPool}.OnReturn({collection})",
                        $"{OBJECT_POOL}.ObjectPool(System.Func<T>, System.Action<T>?, System.Action<T>?, System.Action<T>?)",
                        $"{OBJECT_POOL}.Return(T)"
                    ]
                },
                new MethodSource
                {
                    Name = "OnCreate",
                    Signature = $"private static {globalCollection} OnCreate()",
                    Implementation = (writer, in context) => { writer.AppendLine($"return new {globalCollection}();"); },
                    SkipPartial = true,
                    EmptyStub = "return null!;"
                },
                new MethodSource
                {
                    Name = "OnReturn",
                    Signature = $"private static void OnReturn({globalCollection} item)",
                    Implementation = (writer, in context) => { writer.AppendLine("item.Clear();"); },
                    SkipPartial = true
                }
            ]
        };
    }
}