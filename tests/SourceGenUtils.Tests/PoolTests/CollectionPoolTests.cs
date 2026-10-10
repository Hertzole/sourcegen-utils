using System;
using System.Collections.Generic;
using System.Text;
using Hertzole.SourceGen.Wrappers;
using NUnit.Framework;

namespace SourceGenUtils.Tests.PoolTests;

[TestFixture(typeof(List<object>))]
[TestFixture(typeof(HashSet<object>))]
[TestFixture(typeof(Queue<object>))]
[TestFixture(typeof(Stack<object>))]
[TestFixture(typeof(StringBuilder))]
public class CollectionPoolTests<T> : GeneratorTests where T : class
{
    private readonly string typeName;
    private readonly string itemTypeName;

    public CollectionPoolTests()
    {
        if (typeof(T) == typeof(List<object>))
        {
            typeName = "ListPool";
            itemTypeName = "System.Collections.Generic.List<object>";
            return;
        }

        if (typeof(T) == typeof(HashSet<object>))
        {
            typeName = "HashSetPool";
            itemTypeName = "System.Collections.Generic.HashSet<object>";
            return;
        }

        if (typeof(T) == typeof(Queue<object>))
        {
            typeName = "QueuePool";
            itemTypeName = "System.Collections.Generic.Queue<object>";
            return;
        }

        if (typeof(T) == typeof(Stack<object>))
        {
            typeName = "StackPool";
            itemTypeName = "System.Collections.Generic.Stack<object>";
            return;
        }

        if (typeof(T) == typeof(StringBuilder))
        {
            typeName = "StringBuilderPool";
            itemTypeName = "System.Text.StringBuilder";
            return;
        }

        throw new ArgumentException($"Unsupported type {typeof(T).FullName}");
    }

    [Test]
    public void Get()
    {
        // Arrange
        Pool pool = CompilePool();

        // Act
        T item1 = pool.Get();
        pool.Return(item1);
        T item2 = pool.Get();

        // Assert
        Assert.That(item2, Is.Not.Null);
        Assert.That(item2, Is.SameAs(item1));
    }

    [Test]
    public void GetScope()
    {
        // Arrange
        Pool pool = CompilePool();

        // Act
        Scope scope = pool.GetScope(out T item1);
        scope.Dispose();
        using Scope scope2 = pool.GetScope(out T item2);

        // Assert
        Assert.That(item2, Is.Not.Null);
        Assert.That(item2, Is.SameAs(item1));
    }

    [Test]
    public void Get_IsCleared()
    {
        // Arrange
        Pool pool = CompilePool();

        // Act
        T item1 = pool.Get();
        for (int i = 0; i < 16; i++)
        {
            Add(item1, new object());
        }

        pool.Return(item1);

        T item2 = pool.Get();

        // Assert
        Assert.That(item2, Is.Not.Null);
        Assert.That(item2, Is.SameAs(item1));
        Assert.That(IsCleared(item2), Is.True);
    }

    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return typeName;
    }

    private Pool CompilePool()
    {
        string poolRef = typeof(T) == typeof(StringBuilder) ? typeName : $"{typeName}<object>";
        string usage =
            $"var item = {poolRef}.Get(); {poolRef}.Return(item); {poolRef}.Get(out {itemTypeName} item2).Dispose();";

        if (typeof(T) == typeof(StringBuilder))
        {
            StringBuilderPool pool = new StringBuilderPool(CompileGeneratedTypeByUsing("StringBuilderPool", usage));
            return Capture(() => pool.Get(), (out item) => new Scope(pool.Get(out item)), pool.Return);
        }

        Type poolType = CompileGeneratedTypeByUsing($"{typeName}`1", usage).MakeGenericType(typeof(object));

        if (typeof(T) == typeof(List<object>))
        {
            ListPool<object> pool = new ListPool<object>(poolType);
            return Capture(() => pool.Get(), (out item) => new Scope(pool.Get(out item)), pool.Return);
        }

        if (typeof(T) == typeof(HashSet<object>))
        {
            HashSetPool<object> pool = new HashSetPool<object>(poolType);
            return Capture(() => pool.Get(), (out item) => new Scope(pool.Get(out item)), pool.Return);
        }

        if (typeof(T) == typeof(Queue<object>))
        {
            QueuePool<object> pool = new QueuePool<object>(poolType);
            return Capture(() => pool.Get(), (out item) => new Scope(pool.Get(out item)), pool.Return);
        }

        if (typeof(T) == typeof(Stack<object>))
        {
            StackPool<object> pool = new StackPool<object>(poolType);
            return Capture(() => pool.Get(), (out item) => new Scope(pool.Get(out item)), pool.Return);
        }

        throw new ArgumentException($"Unsupported type {typeof(T).FullName}");
    }

    private static Pool Capture<TItem>(Func<TItem> get, GetScopeBridge<TItem> getScope, Action<TItem> @return)
        where TItem : class
    {
        return new Pool
        {
            Get = () => (T) (object) get(),
            GetScope = (out item) =>
            {
                Scope scope = getScope(out TItem value);
                item = (T) (object) value;
                return scope;
            },
            Return = item => @return((TItem) (object) item!)
        };
    }

    private static void Add(T collection, object item)
    {
        switch (collection)
        {
            case IList<object> list:
                list.Add(item);
                break;
            case ISet<object> set:
                set.Add(item);
                break;
            case Queue<object> queue:
                queue.Enqueue(item);
                break;
            case Stack<object> stack:
                stack.Push(item);
                break;
            case StringBuilder sb:
                sb.Append(item);
                break;
            default:
                throw new ArgumentException("Unsupported collection type");
        }
    }

    private static bool IsCleared(T value)
    {
        if (value is IReadOnlyCollection<object> col)
        {
            return col.Count == 0;
        }

        if (value is StringBuilder sb)
        {
            return sb.Length == 0;
        }

        return false;
    }

    private delegate Scope GetScopeBridge<TItem>(out TItem item) where TItem : class;

    private sealed class Pool
    {
        public required Func<T> Get { get; init; }
        public required GetScopeDelegate GetScope { get; init; }
        public required Action<T> Return { get; init; }
    }

    private delegate Scope GetScopeDelegate(out T item);

    private readonly struct Scope : IDisposable
    {
        private readonly IDisposable inner;

        public Scope(IDisposable inner)
        {
            this.inner = inner;
        }

        public void Dispose()
        {
            inner.Dispose();
        }
    }
}