using System;
using System.Collections.Generic;
using System.Reflection;
using Hertzole.SourceGenUtils;
using NUnit.Framework;

namespace SourceGenUtils.Tests.PoolTests;

[TestFixture(typeof(List<object>))]
[TestFixture(typeof(HashSet<object>))]
[TestFixture(typeof(Queue<object>))]
[TestFixture(typeof(Stack<object>))]
public class CollectionPoolTests<T> : GeneratorTests where T : IReadOnlyCollection<object>
{
    private readonly string typeName;
    private readonly string genericTypeName;

    public CollectionPoolTests()
    {
        if (typeof(T) == typeof(List<object>))
        {
            typeName = "ListPool";
            genericTypeName = "System.Collections.Generic.List<T>";
            return;
        }

        if (typeof(T) == typeof(HashSet<object>))
        {
            typeName = "HashSetPool";
            genericTypeName = "System.Collections.Generic.HashSet<T>";
            return;
        }

        if (typeof(T) == typeof(Queue<object>))
        {
            typeName = "QueuePool";
            genericTypeName = "System.Collections.Generic.Queue<T>";
            return;
        }

        if (typeof(T) == typeof(Stack<object>))
        {
            typeName = "StackPool";
            genericTypeName = "System.Collections.Generic.Stack<T>";
            return;
        }

        throw new ArgumentException($"Unsupported type {typeof(T).FullName}");
    }

    [Test]
    public void Get()
    {
        // Arrange
        PoolWrapper pool = CompilePool("Get()", $"Return({genericTypeName})");

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
        PoolWrapper pool = CompilePool($"Get({genericTypeName})", $"Return({genericTypeName})");

        // Act
        PoolScopeWrapper scope = pool.Get(out T item1);
        scope.Dispose();
        using PoolScopeWrapper scope2 = pool.Get(out T item2);

        // Assert
        Assert.That(item2, Is.Not.Null);
        Assert.That(item2, Is.SameAs(item1));
    }

    [Test]
    public void Get_IsCleared()
    {
        // Arrange
        PoolWrapper pool = CompilePool("Get()", $"Return({genericTypeName})");

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
        Assert.That(item2.Count, Is.EqualTo(0));
    }

    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return typeName;
    }

    private PoolWrapper CompilePool(params string[] calledMethods)
    {
        return new PoolWrapper(typeName, calledMethods);
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
            default:
                throw new ArgumentException("Unsupported collection type");
        }
    }

    private sealed class PoolWrapper
    {
        private readonly MethodInfo getMethod;
        private readonly MethodInfo getOutMethod;
        private readonly MethodInfo returnMethod;

        public PoolWrapper(string typeName, params string[] calledMethods)
        {
            Assembly asm = CompileAssembly(AppendTypeIfNeeded(typeName, calledMethods));
            Type type = asm.GetType($"{Generator.NAMESPACE}.{typeName}`1", true)!.MakeGenericType(typeof(object));

            getMethod = GetMethod(type, "Get", BindingFlags.Public | BindingFlags.Static, Array.Empty<Type>());
            getOutMethod = GetMethod(type, "Get", BindingFlags.Public | BindingFlags.Static, typeof(T).MakeByRefType());
            returnMethod = GetMethod(type, "Return", BindingFlags.Public | BindingFlags.Static, typeof(T));
        }

        public T Get()
        {
            object? item = getMethod.InvokeStatic();

            Assert.That(item, Is.Not.Null, "Item should not be null");

            return (T) item!;
        }

        public PoolScopeWrapper Get(out T item)
        {
            object?[] args = [null];
            object? scope = getOutMethod.InvokeStatic(args);
            item = (T) args[0]!;
            return new PoolScopeWrapper(scope!);
        }

        public void Return(T item)
        {
            returnMethod.InvokeStatic(item);
        }
    }
}