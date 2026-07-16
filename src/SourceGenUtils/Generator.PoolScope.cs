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
            Trivia = new TriviaSource
            {
                Summary = "A disposable scope that automatically returns a pooled object when disposed."
            },
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
                    Dependencies = [pool_scope + ".Dispose()"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Creates a new pool scope that will return the specified item to the pool when disposed.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["item"] = "The pooled object to return on disposal.",
                            ["pool"] = "The pool to return the object to."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "Dispose",
                    Signature = "public partial void Dispose()",
                    Implementation = (writer, in context) => { writer.AppendLine("pool.Return(item);"); },
                    Trivia = new TriviaSource
                    {
                        Summary = "Returns the pooled object back to the pool."
                    }
                }
            ]
        };
    }
}