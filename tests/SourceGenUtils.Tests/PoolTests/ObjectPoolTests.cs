using System;
using System.Reflection;
using Hertzole.SourceGenUtils;
using NUnit.Framework;

namespace SourceGenUtils.Tests.PoolTests;

public class ObjectPoolTests : GeneratorTests
{
    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "ObjectPool";
    }

    [Test]
    public void Get()
    {
        // Arrange
        using ObjectPoolWrapper pool = CompileObjectPool(calledMethods: ["Get()", "Return(T)"]);

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
        using ObjectPoolWrapper pool = CompileObjectPool(calledMethods: ["Get(T)", "Return(T)"]);

        // Act
        PoolScopeWrapper scope = pool.Get(out object item1);
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
        using ObjectPoolWrapper pool = CompileObjectPool(() =>
        {
            factoryCalled = true;
            return new object();
        }, calledMethods: ["Get()", "Return(T)"]);

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
        using ObjectPoolWrapper pool = CompileObjectPool(onGet: o =>
        {
            onGetCalled = true;
            onGetItem = o;
        }, calledMethods: ["Get()", "Return(T)"]);

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
        using ObjectPoolWrapper pool = CompileObjectPool(onReturn: o =>
        {
            onReturnCalled = true;
            onReturnItem = o;
        }, calledMethods: ["Get()", "Return(T)"]);

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
        ObjectPoolWrapper pool = CompileObjectPool(onDispose: o =>
        {
            onDisposeCalled = true;
            onDisposeItem = o;
        }, calledMethods: ["Get()", "Return(T)"]);

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

    private static ObjectPoolWrapper CompileObjectPool(Func<object>? create = null,
        Action<object>? onGet = null,
        Action<object>? onReturn = null,
        Action<object>? onDispose = null,
        params string[] calledMethods)
    {
        string[] called = ["ObjectPool.ObjectPool(System.Func<T>, System.Action<T>, System.Action<T>, System.Action<T>)", .. calledMethods];

        Assembly asm = CompileAssembly(AppendTypeIfNeeded("ObjectPool", called));
        Type? type = asm.GetType($"{Generator.NAMESPACE}.ObjectPool`1");

        Assert.That(type, Is.Not.Null, "Couldn't find object pool type in assembly.");

        Type typeWithGeneric = type!.MakeGenericType(typeof(object));

        create ??= () => new object();

        object instance = CreateInstance(typeWithGeneric, create, onGet, onReturn, onDispose);

        return new ObjectPoolWrapper(instance);
    }

    private sealed class ObjectPoolWrapper : IDisposable
    {
        private readonly object instance;
        private readonly MethodInfo getMethod;
        private readonly MethodInfo getOutMethod;
        private readonly MethodInfo returnMethod;
        private readonly MethodInfo disposeMethod;

        public ObjectPoolWrapper(object instance)
        {
            this.instance = instance;
            Type type = instance.GetType();

            getMethod = GetMethod(type, "Get", BindingFlags.Public | BindingFlags.Instance, Array.Empty<Type>());
            getOutMethod = GetMethod(type, "Get", BindingFlags.Public | BindingFlags.Instance, typeof(object).MakeByRefType());
            returnMethod = GetMethod(type, "Return", BindingFlags.Public | BindingFlags.Instance, typeof(object));
            disposeMethod = GetMethod(type, "Dispose", BindingFlags.Public | BindingFlags.Instance);
        }

        public object Get()
        {
            object? item = getMethod.InvokeInstance(instance);

            Assert.That(item, Is.Not.Null, "Item should not be null");

            return item!;
        }

        public PoolScopeWrapper Get(out object item)
        {
            object?[] args = [null];
            object? scope = getOutMethod.InvokeInstance(instance, args);
            item = args[0]!;
            return new PoolScopeWrapper(scope!);
        }

        public void Return(object item)
        {
            returnMethod.InvokeInstance(instance, item);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            disposeMethod.InvokeInstance(instance);
        }
    }

    private readonly struct PoolScopeWrapper : IDisposable
    {
        private readonly object instance;
        private readonly MethodInfo disposeMethod;

        public PoolScopeWrapper(object instance)
        {
            this.instance = instance;

            disposeMethod = GetMethod(instance.GetType(), "Dispose", BindingFlags.Public | BindingFlags.Instance);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            disposeMethod.InvokeInstance(instance);
        }
    }
}