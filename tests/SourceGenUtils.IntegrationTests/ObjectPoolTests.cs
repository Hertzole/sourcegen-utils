using Hertzole.SourceGen;

namespace Hertzole.SourceGenUtils.IntegrationTests;

public class ObjectPoolTests
{
    [Test]
    public void Get()
    {
        // Arrange
        using ObjectPool<object> pool = new ObjectPool<object>(() => new object());

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
        using ObjectPool<object> pool = new ObjectPool<object>(() => new object());

        // Act
        PoolScope<object> scope = pool.Get(out object item1);
        scope.Dispose();
        object item2 = pool.Get();

        // Assert
        Assert.That(item2, Is.Not.Null);
        Assert.That(item2, Is.SameAs(item1));
    }

    [Test]
    public void FactoryCallback()
    {
        // Arrange
        bool factoryCalled = false;
        using ObjectPool<object> pool = new ObjectPool<object>(() =>
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
        using ObjectPool<object> pool = new ObjectPool<object>(() => new object(), o =>
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
        using ObjectPool<object> pool = new ObjectPool<object>(() => new object(), onReturn: o =>
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
        ObjectPool<object> pool = new ObjectPool<object>(() => new object(), onDispose: o =>
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
}