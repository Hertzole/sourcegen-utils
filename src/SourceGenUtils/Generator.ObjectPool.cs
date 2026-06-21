using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private const string OBJECT_POOL = NAMESPACE + ".ObjectPool";

    private static TypeSource CreateObjectPool()
    {
        return new TypeSource
        {
            Signature = "internal sealed partial class ObjectPool<T> : global::System.IDisposable where T : class",
            Fields = new Dictionary<string, FieldSource>
            {
                ["pool"] = new FieldSource
                {
                    Signature = "private readonly global::System.Collections.Generic.Stack<T> pool = new global::System.Collections.Generic.Stack<T>();",
                    Dependencies = [OBJECT_POOL + ".ObjectPool"]
                },
                ["create"] = new FieldSource
                {
                    Signature = "private readonly global::System.Func<T> create;",
                    Dependencies = [OBJECT_POOL + ".ObjectPool"]
                },
                ["onGet"] = new FieldSource
                {
                    Signature = "private readonly global::System.Action<T>? onGet;",
                    Dependencies = [OBJECT_POOL + ".ObjectPool"]
                },
                ["onReturn"] = new FieldSource
                {
                    Signature = "private readonly global::System.Action<T>? onReturn;",
                    Dependencies = [OBJECT_POOL + ".ObjectPool"]
                },
                ["onDispose"] = new FieldSource
                {
                    Signature = "private readonly global::System.Action<T>? onDispose;",
                    Dependencies = [OBJECT_POOL + ".ObjectPool"]
                }
            },
            Methods =
            [
                new MethodSource
                {
                    Name = "ObjectPool",
                    Signature =
                        "public partial ObjectPool(global::System.Func<T> create, global::System.Action<T>? onGet = null, global::System.Action<T>? onReturn = null, global::System.Action<T>? onDispose = null)",
                    Implementation = (writer, in context) =>
                    {
                        writer.AppendLine("this.create = create;");
                        writer.AppendLine("this.onGet = onGet;");
                        writer.AppendLine("this.onReturn = onReturn;");
                        writer.AppendLine("this.onDispose = onDispose;");
                    },
                    Dependencies = [OBJECT_POOL + ".Dispose()"]
                },
                new MethodSource
                {
                    Name = "Get",
                    Signature = "public partial T Get()",
                    EmptyStub = "return default;",
                    Implementation = (writer, in context) =>
                    {
                        writer.AppendLine("T result;");
                        writer.AppendLine("if (pool.Count > 0)");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("result = pool.Pop();");
                        }

                        writer.AppendLine("else");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("result = create.Invoke();");
                        }

                        writer.AppendLine();
                        writer.AppendLine("onGet?.Invoke(result);");
                        writer.AppendLine("return result;");
                    }
                },
                new MethodSource
                {
                    Name = "Get",
                    Signature = "public partial global::" + NAMESPACE + ".PoolScope<T> Get(out T item)",
                    EmptyStub = "item = default; return default;",
                    Implementation = (writer, in context) =>
                    {
                        writer.AppendLine("item = Get();");
                        writer.AppendLine("return new global::" + NAMESPACE + ".PoolScope<T>(item, this);");
                    },
                    Dependencies = [NAMESPACE + ".PoolScope.PoolScope(T, ObjectPool<T>)", OBJECT_POOL + ".Get()", OBJECT_POOL + ".Return(T)"]
                },
                new MethodSource
                {
                    Name = "Return",
                    Signature = "public partial void Return(T value)",
                    Implementation = (writer, in context) =>
                    {
                        writer.AppendLine("onReturn?.Invoke(value);");
                        writer.AppendLine("pool.Push(value);");
                    }
                },
                new MethodSource
                {
                    Name = "Dispose",
                    Signature = "public partial void Dispose()",
                    Implementation = (writer, in context) =>
                    {
                        writer.AppendLine("global::System.GC.SuppressFinalize(this);");
                        writer.AppendLine("if (onDispose != null)");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("while (pool.Count > 0)");
                            using (writer.WithBlock())
                            {
                                writer.AppendLine("onDispose.Invoke(pool.Pop());");
                            }
                        }

                        writer.AppendLine();
                        writer.AppendLine("pool.Clear();");
                    }
                },
                new MethodSource
                {
                    Name = "Finalizer",
                    Signature = "~ObjectPool()",
                    Implementation = (writer, in context) => { writer.AppendLine("Dispose();"); },
                    AlwaysWrite = true,
                    SkipPartial = true
                }
            ]
        };
    }
}