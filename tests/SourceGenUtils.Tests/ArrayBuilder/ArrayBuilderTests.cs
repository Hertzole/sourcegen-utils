using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Hertzole.SourceGen.Wrappers;
using Hertzole.SourceGenUtils;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

public class ArrayBuilderTests : GeneratorTests
{
    [Test]
    public void Add()
    {
        // Arrange
        using ArrayBuilder<char> builder = GetWrapper<char>("new ArrayBuilder<char>().Add('a');");
        char value = Fake.Random.Char();

        // Act
        builder.Add(value);

        // Assert
        Assert.That(builder.Count, Is.EqualTo(1));
        Assert.That(builder[0], Is.EqualTo(value));
    }

    [Test]
    public void AddRange_Enumerable_List()
    {
        // Arrange
        List<char> values = Fake.Random.Chars(count: 32).ToList();

        // Act
        AddRangeTest(values);
    }

    [Test]
    public void AddRange_Enumerable_Enumerable()
    {
        // Arrange
        Stack<char> values = new Stack<char>(Fake.Random.Chars(count: 32));

        // Act
        AddRangeTest(values);
    }

    private static void AddRangeTest(IEnumerable<char> values)
    {
        // Arrange
        using ArrayBuilder<char> builder = GetWrapper<char>("new ArrayBuilder<char>().AddRange(new List<char>());");

        // Act
        IEnumerable<char> enumerable = values.ToList();
        builder.AddRange(enumerable);

        // Assert
        char[] array = enumerable.ToArray();
        Assert.That(builder.Count, Is.EqualTo(array.Length));
        for (int i = 0; i < array.Length; i++)
        {
            Assert.That(builder[i], Is.EqualTo(array[i]));
        }
    }

    [Test]
    public void AddRange_String()
    {
        // Arrange
        using ArrayBuilder<char> builder =
            GetWrapper<char>("var builder = new ArrayBuilder<char>(); ArrayBuilderExtensions.AddRange(builder, \"\"); builder.ToString();");

        ArrayBuilderExtensions extensions = new ArrayBuilderExtensions(builder.Type.Assembly);
        string message = Fake.Lorem.Sentence();

        // Act
        extensions.AddRange(builder, message);

        // Assert
        Assert.That(builder.ToString(), Is.EqualTo(message));
    }

    [Test]
    public void Remove()
    {
        // Arrange
        using ArrayBuilder<char> builder =
            GetWrapper<char>(
                "var builder = new ArrayBuilder<char>(); builder.AddRange(ReadOnlySpan<char>.Empty); builder.Remove('a'); builder.ToString(); builder.Contains('a');");

        char[] values = "abcdefg".ToCharArray();
        char toRemove = 'b';
        builder.AddRange(values);

        // Act
        bool result = builder.Remove(toRemove);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(builder.Count, Is.EqualTo(values.Length - 1));
        Assert.That(builder.Contains(toRemove), Is.False);
    }

    [Test]
    public void RemoveAt()
    {
        // Arrange
        using ArrayBuilder<char> builder =
            GetWrapper<char>(
                "var builder = new ArrayBuilder<char>(); builder.AddRange(ReadOnlySpan<char>.Empty); builder.RemoveAt(0); builder.Contains('a'); builder.ToString(); var x = builder[0];");

        char[] values = "abcdefg".ToCharArray();
        int toRemove = 2;
        builder.AddRange(values);

        // Act
        builder.RemoveAt(toRemove);

        // Assert
        Assert.That(builder.Count, Is.EqualTo(values.Length - 1));
        Assert.That(builder[toRemove], Is.Not.EqualTo(values[toRemove]));
        Assert.That(builder.Contains(values[toRemove]), Is.False);
    }

    [Test]
    public void IndexOf_Valid()
    {
        // Arrange
        using ArrayBuilder<int> builder =
            GetWrapper<int>("var builder = new ArrayBuilder<int>(); builder.AddRange(ReadOnlySpan<int>.Empty); builder.IndexOf(0);");

        builder.AddRange([1, 2, 3, 4, 5]);

        // Act
        int index = builder.IndexOf(3);

        // Assert
        Assert.That(index, Is.EqualTo(2));
    }

    [Test]
    public void IndexOf_Invalid()
    {
        // Arrange
        using ArrayBuilder<int> builder =
            GetWrapper<int>("var builder = new ArrayBuilder<int>(); builder.AddRange(ReadOnlySpan<int>.Empty); builder.IndexOf(0);");

        builder.AddRange([1, 2, 3, 4, 5]);

        // Act
        int index = builder.IndexOf(8);

        // Assert
        Assert.That(index, Is.EqualTo(-1));
    }

    [Test]
    public void Contains_True()
    {
        // Arrange
        using ArrayBuilder<int> builder =
            GetWrapper<int>(@"var builder = new ArrayBuilder<int>(); builder.AddRange(ReadOnlySpan<int>.Empty); builder.Contains(0);");

        builder.AddRange([1, 2, 3, 4, 5]);

        // Act
        bool found = builder.Contains(3);

        // Assert
        Assert.That(found, Is.True);
    }

    [Test]
    public void Contains_False()
    {
        // Arrange
        using ArrayBuilder<int> builder =
            GetWrapper<int>(@"var builder = new ArrayBuilder<int>(); builder.AddRange(ReadOnlySpan<int>.Empty); builder.Contains(0);");

        builder.AddRange([1, 2, 3, 4, 5]);

        // Act
        bool found = builder.Contains(8);

        // Assert
        Assert.That(found, Is.False);
    }

    [Test]
    public void Clear()
    {
        // Arrange
        using ArrayBuilder<char> builder =
            GetWrapper<char>("var builder = new ArrayBuilder<char>(); builder.AddRange(ReadOnlySpan<char>.Empty); builder.Clear();");

        builder.AddRange(Fake.Random.Chars(count: 32));

        // Act
        builder.Clear();

        // Assert
        Assert.That(builder.Count, Is.EqualTo(0));
    }

    [Test]
    public void ToArray()
    {
        // Arrange
        using ArrayBuilder<char> builder =
            GetWrapper<char>(@"var builder = new ArrayBuilder<char>(); builder.AddRange(ReadOnlySpan<char>.Empty); builder.ToArray();");

        char[] values = Fake.Random.Chars(count: 32);
        builder.AddRange(values);

        // Act
        char[] actual = builder.ToArray();

        // Assert
        Assert.That(actual, Is.EqualTo(values));
    }

    [Test]
    [TestCase(1)]
    [TestCase(10)]
    [TestCase(32)]
    [TestCase(69)]
    [TestCase(322)]
    public void ToImmutableArray(int count)
    {
        // Arrange
        using ArrayBuilder<char> builder =
            GetWrapper<char>(@"var builder = new ArrayBuilder<char>(); builder.AddRange(ReadOnlySpan<char>.Empty); builder.ToImmutableArray();");

        char[] values = Fake.Random.Chars(count: count);
        builder.AddRange(values);

        // Act
        ImmutableArray<char> actual = builder.ToImmutableArray();

        // Assert
        Assert.That(actual, Is.EqualTo(values));
    }

    [Test]
    public void ToString_Chars()
    {
        // Arrange
        using ArrayBuilder<char> builder =
            GetWrapper<char>("var builder = new ArrayBuilder<char>(); builder.AddRange(ReadOnlySpan<char>.Empty); builder.ToString();");

        char[] values = Fake.Random.Chars(count: 32);
        string expected = new string(values);
        builder.AddRange(values);

        // Act
        string actual = builder.ToString();

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ToString_Others()
    {
        // Arrange
        using ArrayBuilder<byte> builder =
            GetWrapper<byte>("var builder = new ArrayBuilder<byte>(); builder.AddRange(ReadOnlySpan<byte>.Empty); builder.ToString();");

        byte[] values = Fake.Random.Bytes(32);
        builder.AddRange(values);

        // Act
        string actual = builder.ToString();

        // Assert
        Assert.That(actual, Is.EqualTo($"{Constants.ARRAY_BUILDER}<{nameof(Byte)}>[{values.Length}]"));
    }

    [Test]
    public void Dispose_PoolsArray()
    {
        // Arrange
        ArrayBuilder<byte> builder = GetWrapper<byte>(@"var builder = new ArrayBuilder<byte>(); builder.AddRange(ReadOnlySpan<byte>.Empty);");
        builder.AddRange(Fake.Random.Bytes(32));
        byte[] internalArray = builder.writer.array;

        // Act
        builder.Dispose();

        // Assert
        for (int i = 0; i < 32; i++)
        {
            Assert.That(internalArray[i], Is.EqualTo(0));
        }

        Assert.That(builder.Count, Is.EqualTo(0));
        Assert.That(builder.writer.array, Is.Empty);
    }

    [Test]
    public void Indexer_Get()
    {
        // Arrange
        using ArrayBuilder<int> builder = GetWrapper<int>("var builder = new ArrayBuilder<int>(); builder.Add(0); var x = builder[0];");
        int value = Fake.Random.Int();
        builder.Add(value);

        // Act
        int result = builder[0];

        // Assert
        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void Indexer_Set()
    {
        // Arrange
        using ArrayBuilder<int> builder = GetWrapper<int>(@"var builder = new ArrayBuilder<int>(); builder.Add(0); builder[0] = 69;");
        int value = Fake.Random.Int();
        int setValue = value + 69;
        builder.Add(value);

        // Act
        builder[0] = setValue;

        // Assert
        Assert.That(builder[0], Is.EqualTo(setValue));
    }

    [Test]
    [TestCase(-1)]
    [TestCase(1)]
    [TestCase(10)]
    public void Indexer_Get_OutOfRange_ThrowsException(int index)
    {
        // Arrange
        ArrayBuilder<int> builder = GetWrapper<int>(@"var builder = new ArrayBuilder<int>(); builder.Add(0); var x = builder[0];");
        int value = Fake.Random.Int();
        builder.Add(value);

        // Act & Assert
        var exception = Assert.Throws<TargetInvocationException>(() =>
        {
            int _ = builder[index];
        });

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    [TestCase(-1)]
    [TestCase(1)]
    [TestCase(10)]
    public void Indexer_Set_OutOfRange_ThrowsException(int index)
    {
        // Arrange
        ArrayBuilder<int> builder = GetWrapper<int>(@"var builder = new ArrayBuilder<int>(); builder.Add(0); builder[0] = 69;");
        int value = Fake.Random.Int();
        builder.Add(value);

        // Act & Assert
        TargetInvocationException? exception = Assert.Throws<TargetInvocationException>(() => { builder[index] = 420; });
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
    }

    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "ArrayBuilder";
    }

    private static ArrayBuilder<T> GetWrapper<T>(string useMethods)
    {
        Type type = CompileGeneratedTypeByUsing("ArrayBuilder`1", useMethods).MakeGenericType(typeof(T));
        return new ArrayBuilder<T>(type);
    }

    // public struct ArrayBuilder<T> : IDisposable
    // {
    //     public readonly object Instance;
    //     private readonly PropertyInfo lengthProperty;
    //     private readonly PropertyInfo indexer;
    //     private readonly MethodInfo addMethod;
    //     private readonly MethodInfo addRangeIEnumerable;
    //     private readonly MethodInfo removeMethod;
    //     private readonly MethodInfo removeAtMethod;
    //     private readonly MethodInfo indexOfMethod;
    //     private readonly MethodInfo containsMethod;
    //     private readonly MethodInfo clearMethod;
    //     private readonly MethodInfo disposeMethod;
    //     private readonly MethodInfo toArrayMethod;
    //     private readonly MethodInfo toImmutableArrayMethod;
    //     private readonly MethodInfo toString;
    //     private readonly FieldInfo internalArray;
    //     private readonly FieldInfo writerField;
    //
    //     public readonly int Length
    //     {
    //         get { return (int) lengthProperty.GetValue(Instance)!; }
    //     }
    //
    //     public readonly T[] InternalArray
    //     {
    //         get { return (T[]) internalArray.GetValue(writerField.GetValue(Instance))!; }
    //     }
    //
    //     public ArrayBuilder(object instance)
    //     {
    //         Instance = instance;
    //         Type type = instance.GetType();
    //
    //         lengthProperty = GetProperty(type, "Count", BindingFlags.Public | BindingFlags.Instance)!;
    //         indexer = GetProperty(type, "Item", BindingFlags.Public | BindingFlags.Instance)!;
    //         addMethod = GetMethod(type, "Add", BindingFlags.Public | BindingFlags.Instance);
    //         addRangeIEnumerable = GetMethod(type, "AddRange", BindingFlags.Public | BindingFlags.Instance, typeof(IEnumerable<T>));
    //         removeMethod = GetMethod(type, "Remove", BindingFlags.Public | BindingFlags.Instance);
    //         removeAtMethod = GetMethod(type, "RemoveAt", BindingFlags.Public | BindingFlags.Instance);
    //         indexOfMethod = GetMethod(type, "IndexOf", BindingFlags.Public | BindingFlags.Instance);
    //         containsMethod = GetMethod(type, "Contains", BindingFlags.Public | BindingFlags.Instance);
    //         clearMethod = GetMethod(type, "Clear", BindingFlags.Public | BindingFlags.Instance);
    //         disposeMethod = GetMethod(type, "Dispose", BindingFlags.Public | BindingFlags.Instance);
    //         toArrayMethod = GetMethod(type, "ToArray", BindingFlags.Public | BindingFlags.Instance);
    //         toImmutableArrayMethod = GetMethod(type, "ToImmutableArray", BindingFlags.Public | BindingFlags.Instance);
    //         toString = GetMethod(type, "ToString", BindingFlags.Public | BindingFlags.Instance);
    //         writerField = GetField(type, "writer", BindingFlags.Instance | BindingFlags.NonPublic);
    //
    //         Type writerType = writerField.FieldType;
    //         internalArray = GetField(writerType!, "array", BindingFlags.NonPublic | BindingFlags.Instance);
    //     }
    //
    //     public readonly T this[int index]
    //     {
    //         get { return (T) indexer.GetMethod!.InvokeInstance(Instance, index)!; }
    //         set { indexer.SetMethod!.InvokeInstance(Instance, index, value); }
    //     }
    //
    //     public readonly void Add(T value)
    //     {
    //         addMethod.InvokeInstance(Instance, value);
    //     }
    //
    //     public void AddRange(IEnumerable<T> values)
    //     {
    //         addRangeIEnumerable.InvokeInstance(Instance, values);
    //     }
    //
    //     public bool Remove(T value)
    //     {
    //         return removeMethod.InvokeInstance<bool>(Instance, value);
    //     }
    //
    //     public void RemoveAt(int index)
    //     {
    //         removeAtMethod.InvokeInstance(Instance, index);
    //     }
    //
    //     public int IndexOf(T value)
    //     {
    //         return (int) indexOfMethod.InvokeInstance(Instance, value)!;
    //     }
    //
    //     public bool Contains(T value)
    //     {
    //         return (bool) containsMethod.InvokeInstance(Instance, value)!;
    //     }
    //
    //     public void Clear()
    //     {
    //         clearMethod.InvokeInstance(Instance);
    //     }
    //
    //     public T[] ToArray()
    //     {
    //         return (T[]) toArrayMethod.InvokeInstance(Instance)!;
    //     }
    //
    //     public ImmutableArray<T> ToImmutableArray()
    //     {
    //         return (ImmutableArray<T>) toImmutableArrayMethod.InvokeInstance(Instance)!;
    //     }
    //
    //     /// <inheritdoc />
    //     public override string ToString()
    //     {
    //         return (string) toString.InvokeInstance(Instance)!;
    //     }
    //
    //     /// <inheritdoc />
    //     public void Dispose()
    //     {
    //         disposeMethod.InvokeInstance(Instance);
    //     }
    // }
}