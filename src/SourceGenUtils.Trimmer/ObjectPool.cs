using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hertzole.SourceGen;

[GeneratedCode("Hertzole.SourceGenUtils.Generator", "1.0.0.0")]
[ExcludeFromCodeCoverage]
internal sealed class ObjectPool<T> : IDisposable where T : class
{
    private readonly Stack<T> pool = new Stack<T>();
    private readonly Func<T> create;
    private readonly Action<T>? onGet;
    private readonly Action<T>? onReturn;
    private readonly Action<T>? onDispose;

    public ObjectPool(Func<T> create, Action<T>? onGet = null, Action<T>? onReturn = null, Action<T>? onDispose = null)
    {
        this.create = create;
        this.onGet = onGet;
        this.onReturn = onReturn;
        this.onDispose = onDispose;
    }

    public T Get()
    {
        T result;
        if (pool.Count > 0)
        {
            result = pool.Pop();
        }
        else
        {
            result = create.Invoke();
        }

        onGet?.Invoke(result);
        return result;
    }

    public void Return(T value)
    {
        onReturn?.Invoke(value);
        pool.Push(value);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (onDispose != null)
        {
            while (pool.Count > 0)
            {
                onDispose.Invoke(pool.Pop());
            }
        }

        pool.Clear();
    }

    ~ObjectPool()
    {
        Dispose();
    }
}