using System;
using System.Reflection;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

public readonly struct PoolScopeWrapper : IDisposable
{
    private readonly object instance;
    private readonly MethodInfo disposeMethod;

    public PoolScopeWrapper(object instance)
    {
        this.instance = instance;

        disposeMethod = instance.GetType().GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance)!;

        Assert.That(disposeMethod, Is.Not.Null);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        disposeMethod.InvokeInstance(instance);
    }
}