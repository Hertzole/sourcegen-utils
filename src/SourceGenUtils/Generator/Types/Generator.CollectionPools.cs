using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private const string LIST_POOL_EXAMPLE =
        """
        The following example gets a list from the pool, fills it, and returns it when done.

        <code>
        using System.Collections.Generic;
        using Hertzole.SourceGen;

        public static class Collector
        {
            public static void Collect(IEnumerable<string> values)
            {
                // The scope returns the list to the pool when disposed.
                using (ListPool<string>.Get(out List<string> list))
                {
                    list.AddRange(values);

                    foreach (string value in list)
                    {
                        System.Console.WriteLine(value);
                    }
                }
            }
        }
        </code>
        """;

    private const string HASH_SET_POOL_EXAMPLE =
        """
        The following example uses a pooled hash set to remove duplicate values.

        <code>
        using System.Collections.Generic;
        using Hertzole.SourceGen;

        public static class DuplicateRemover
        {
            public static List<string> RemoveDuplicates(IEnumerable<string> values)
            {
                // The scope returns the set to the pool when disposed.
                using (HashSetPool<string>.Get(out HashSet<string> set))
                {
                    set.UnionWith(values);
                    return new List<string>(set);
                }
            }
        }
        </code>
        """;

    private const string STACK_POOL_EXAMPLE =
        """
        The following example uses a pooled stack to process values in reverse order.

        <code>
        using System.Collections.Generic;
        using Hertzole.SourceGen;

        public static class ReverseProcessor
        {
            public static void Process(IEnumerable<int> values)
            {
                // The scope returns the stack to the pool when disposed.
                using (StackPool<int>.Get(out Stack<int> stack))
                {
                    foreach (int value in values)
                    {
                        stack.Push(value);
                    }

                    while (stack.Count > 0)
                    {
                        System.Console.WriteLine(stack.Pop());
                    }
                }
            }
        }
        </code>
        """;

    private const string QUEUE_POOL_EXAMPLE =
        """
        The following example uses a pooled queue to process values in order.

        <code>
        using System.Collections.Generic;
        using Hertzole.SourceGen;

        public static class QueueProcessor
        {
            public static void Process(IEnumerable<int> values)
            {
                // The scope returns the queue to the pool when disposed.
                using (QueuePool<int>.Get(out Queue<int> queue))
                {
                    foreach (int value in values)
                    {
                        queue.Enqueue(value);
                    }

                    while (queue.Count > 0)
                    {
                        System.Console.WriteLine(queue.Dequeue());
                    }
                }
            }
        }
        </code>
        """;

    private static TypeSource CreateListPool()
    {
        return CreateCollectionPool("ListPool", "System.Collections.Generic.List<T>", LIST_POOL_EXAMPLE);
    }

    private static TypeSource CreateHashSetPool()
    {
        return CreateCollectionPool("HashSetPool", "System.Collections.Generic.HashSet<T>", HASH_SET_POOL_EXAMPLE);
    }

    private static TypeSource CreateStackPool()
    {
        return CreateCollectionPool("StackPool", "System.Collections.Generic.Stack<T>", STACK_POOL_EXAMPLE);
    }

    private static TypeSource CreateQueuePool()
    {
        return CreateCollectionPool("QueuePool", "System.Collections.Generic.Queue<T>", QUEUE_POOL_EXAMPLE);
    }

    private static TypeSource CreateCollectionPool(string name, string collection, string example)
    {
        string globalCollection = $"global::{collection}";
        string pool = $"global::{NAMESPACE}.ObjectPool<{globalCollection}>";
        string collectionPool = $"{NAMESPACE}.{name}";
        string shortName = collection.Replace("System.Collections.Generic.", string.Empty);
        string collectionTriviaRef = GetTypeTriviaReference(globalCollection.Replace("<T>", "{T}"), shortName, out string displayName);

        return new TypeSource
        {
            Signature = $"internal static partial class {name}<T>",
            Trivia = new TriviaSource
            {
                Summary = $"Provides a shared pool of {collectionTriviaRef} instances to reduce allocations.",
                Example = example
            },
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
                    Implementation = (writer, in _) => { writer.AppendLine("return pool.Get();"); },
                    EmptyStub = "return null!;",
                    Dependencies = CreatePoolGetDependencies(collectionPool),
                    Trivia = new TriviaSource
                    {
                        Summary = $"Retrieves a {collectionTriviaRef} from the pool.",
                        Returns = $"A {collectionTriviaRef} from the pool."
                    }
                },
                new MethodSource
                {
                    Name = "Get",
                    Signature = $"public static partial global::{NAMESPACE}.PoolScope<{globalCollection}> Get(out {globalCollection} item)",
                    Implementation = (writer, in _) => { writer.AppendLine("return pool.Get(out item);"); },
                    EmptyStub = "item = null!; return default;",
                    Dependencies = CreatePoolGetOutDependencies(collectionPool, collection),
                    Trivia = new TriviaSource
                    {
                        Summary = $"Retrieves a {collectionTriviaRef} from the pool and wraps it in a disposable scope that returns it automatically.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["item"] = $"When this method returns, contains the {collectionTriviaRef} retrieved from the pool."
                        },
                        Returns = $"A disposable scope that returns the {collectionTriviaRef} to the pool when disposed."
                    }
                },
                new MethodSource
                {
                    Name = "Return",
                    Signature = $"public static partial void Return({globalCollection} item)",
                    Implementation = (writer, in _) => { writer.AppendLine("pool.Return(item);"); },
                    Dependencies = CreatePoolReturnDependencies(collectionPool, collection),
                    Trivia = new TriviaSource
                    {
                        Summary = $"Returns a {collectionTriviaRef} to the pool.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["item"] = $"The {collectionTriviaRef} to return to the pool."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "OnCreate",
                    Signature = $"private static {globalCollection} OnCreate()",
                    Implementation = (writer, in _) => { writer.AppendLine($"return new {globalCollection}();"); },
                    SkipPartial = true,
                    EmptyStub = "return null!;",
                    Trivia = new TriviaSource
                    {
                        Summary = $"Creates a new empty {collectionTriviaRef}.",
                        Returns = $"A new empty {collectionTriviaRef}."
                    }
                },
                new MethodSource
                {
                    Name = "OnReturn",
                    Signature = $"private static void OnReturn({globalCollection} item)",
                    Implementation = (writer, in _) => { writer.AppendLine("item.Clear();"); },
                    SkipPartial = true,
                    Trivia = new TriviaSource
                    {
                        Summary = "Clears the collection when it is returned to the pool."
                    }
                }
            ]
        };
    }

    private static string[] CreatePoolGetDependencies(string poolName)
    {
        return
        [
            $"{poolName}.OnCreate()",
            $"{OBJECT_POOL}.ObjectPool(System.Func<T>, System.Action<T>, System.Action<T>, System.Action<T>)",
            $"{OBJECT_POOL}.Get()"
        ];
    }

    private static string[] CreatePoolGetOutDependencies(string poolName, string collection)
    {
        return
        [
            $"{poolName}.OnCreate()",
            $"{poolName}.OnReturn({collection})",
            $"{OBJECT_POOL}.ObjectPool(System.Func<T>, System.Action<T>, System.Action<T>, System.Action<T>)",
            $"{OBJECT_POOL}.Get(T)"
        ];
    }

    private static string[] CreatePoolReturnDependencies(string poolName, string collection)
    {
        return
        [
            $"{poolName}.OnReturn({collection})",
            $"{OBJECT_POOL}.ObjectPool(System.Func<T>, System.Action<T>, System.Action<T>, System.Action<T>)",
            $"{OBJECT_POOL}.Return(T)"
        ];
    }
}