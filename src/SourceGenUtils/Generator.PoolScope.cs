using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private static TypeSource CreatePoolScope()
    {
        const string pool_scope = NAMESPACE + ".PoolScope";

        return new TypeSource
        {
            Signature = "internal readonly partial struct PoolScope<T> : global::System.IDisposable where T : class",
            Fields = new Dictionary<string, FieldSource>
            {
                ["item"] = new FieldSource
                {
                    Signature = "private readonly T item;",
                    RequiredDependencies = [pool_scope + ".PoolScope"]
                },
                ["pool"] = new FieldSource
                {
                    Signature = "private readonly global::" + NAMESPACE + ".ObjectPool<T> pool;",
                    RequiredDependencies = [pool_scope + ".PoolScope"]
                }
            },
            Methods =
            [
                new MethodSource
                {
                    Name = "PoolScope",
                    Signature = "public partial PoolScope(T item, ObjectPool<T> pool)",
                    Implementation = (writer, in context) =>
                    {
                        writer.AppendLine("this.item = item;");
                        writer.AppendLine("this.pool = pool;");
                    },
                    Dependencies = [pool_scope + ".Dispose()"]
                },
                new MethodSource
                {
                    Name = "Dispose",
                    Signature = "public partial void Dispose()",
                    Implementation = (writer, in context) => { writer.AppendLine("pool.Return(item);"); }
                }
            ]
        };
    }
}