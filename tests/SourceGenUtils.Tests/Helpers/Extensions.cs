using System.Reflection;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

public static class Extensions
{
    public static object? InvokeInstance(this MethodInfo method, object instance, params object?[] args)
    {
        return method.Invoke(instance, args);
    }

    public static T InvokeInstance<T>(this MethodInfo method, object instance, params object?[] args)
    {
        if (method.ReturnType == typeof(void))
        {
            Assert.Fail("Method does not return a value");
            return default!;
        }

        object? result = method.Invoke(instance, args);

        Assert.That(result, Is.TypeOf<T>());

        return (T) result!;
    }

    public static object? InvokeStatic(this MethodInfo method, params object?[] args)
    {
        return method.Invoke(null, args);
    }

    public static T InvokeStatic<T>(this MethodInfo method, params object?[] args)
    {
        if (method.ReturnType == typeof(void))
        {
            Assert.Fail("Method does not return a value");
            return default!;
        }

        object? result = method.Invoke(null, args);

        Assert.That(result, Is.TypeOf<T>());

        return (T) result!;
    }

    public static T GetValue<T>(this FieldInfo field)
    {
        object? result = field.GetValue(null);

        Assert.That(result, Is.TypeOf<T>());

        return (T) result!;
    }

    public static T GetValue<T>(this FieldInfo field, object instance)
    {
        object? result = field.GetValue(instance);

        Assert.That(result, Is.TypeOf<T>());

        return (T) result!;
    }
}