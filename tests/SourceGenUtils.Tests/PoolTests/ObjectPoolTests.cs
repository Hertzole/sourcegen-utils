using System;
using Hertzole.SourceGen.Wrappers;
using NUnit.Framework;

namespace SourceGenUtils.Tests.PoolTests;

public class ObjectPoolTests : GeneratorTests
{
    private const string POOL_USAGE =
        "var pool = new ObjectPool<object>(() => new object(), null, null, null); " +
        "object item = pool.Get(); pool.Return(item); " +
        "var scope = pool.Get(out object _); scope.Dispose(); " +
        "pool.Dispose();";

    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "ObjectPool";
    }

    [Test]
    public void Get()
    {
        // Arrange
        using ObjectPool<object> pool = CompileObjectPool();

        // Act
        object item1 = pool.Get();
        pool.Return(item1);
        object item2 = pool.Get();

        // Assert
        Assert.That(item2, Is.Not.Null);
        Assert.That(item2, Is.SameAs(item1));
    }

    [Test]
    public void GetScope()
    {
        using ObjectPool<object> pool = CompileObjectPool();

        // Act
        PoolScope<object> scope = pool.Get(out object item1);
        scope.Dispose();
        using PoolScope<object> scope2 = pool.Get(out object item2);

        // Assert
        Assert.That(item2, Is.Not.Null);
        Assert.That(item2, Is.SameAs(item1));
    }

    [Test]
    public void FactoryCallback()
    {
        // Arrange
        bool factoryCalled = false;
        using ObjectPool<object> pool = CompileObjectPool(() =>
        {
            factoryCalled = true;
            return new object();
        });

        // Act
        object item = pool.Get();

        // Assert
        Assert.That(factoryCalled, Is.True);
        Assert.That(item, Is.Not.Null);
    }

    [Test]
    public void OnGetCallback()
    {
        // Arrange
        bool onGetCalled = false;
        object? onGetItem = null;
        using ObjectPool<object> pool = CompileObjectPool(onGet: o =>
        {
            onGetCalled = true;
            onGetItem = o;
        });

        // Act
        object item = pool.Get();

        // Assert
        Assert.That(onGetCalled, Is.True);
        Assert.That(item, Is.Not.Null);
        Assert.That(onGetItem, Is.Not.Null);
        Assert.That(onGetItem, Is.SameAs(item));
    }

    [Test]
    public void OnReturnCallback()
    {
        // Arrange
        bool onReturnCalled = false;
        object? onReturnItem = null;
        using ObjectPool<object> pool = CompileObjectPool(onReturn: o =>
        {
            onReturnCalled = true;
            onReturnItem = o;
        });

        // Act
        object item = pool.Get();
        pool.Return(item);

        // Assert
        Assert.That(onReturnCalled, Is.True);
        Assert.That(item, Is.Not.Null);
        Assert.That(onReturnItem, Is.Not.Null);
        Assert.That(onReturnItem, Is.SameAs(item));
    }

    [Test]
    public void OnDisposeCallback()
    {
        // Arrange
        bool onDisposeCalled = false;
        object? onDisposeItem = null;
        ObjectPool<object> pool = CompileObjectPool(onDispose: o =>
        {
            onDisposeCalled = true;
            onDisposeItem = o;
        });

        // Act
        object item = pool.Get();
        pool.Return(item); // Must return it back so it gets disposed of.
        pool.Dispose();

        // Assert
        Assert.That(onDisposeCalled, Is.True);
        Assert.That(item, Is.Not.Null);
        Assert.That(onDisposeItem, Is.Not.Null);
        Assert.That(onDisposeItem, Is.SameAs(item));
    }

    private static ObjectPool<object> CompileObjectPool(Func<object>? create = null,
        Action<object>? onGet = null,
        Action<object>? onReturn = null,
        Action<object>? onDispose = null)
    {
        create ??= () => new object();

        Type type = CompileGeneratedTypeByUsing("ObjectPool`1", POOL_USAGE).MakeGenericType(typeof(object));
        return new ObjectPool<object>(type, create, onGet, onReturn, onDispose);
    }
}