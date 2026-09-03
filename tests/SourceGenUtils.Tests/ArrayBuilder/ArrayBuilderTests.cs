using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Hertzole.SourceGenUtils;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

public class ArrayBuilderTests : GeneratorTests
{
    [Test]
    public void Add()
    {
        // Arrange
        using ArrayBuilderWrapper<char> builder = CompileWrapper<char>("Add(T)");
        char value = Fake.Random.Char();

        // Act
        builder.Add(value);

        // Assert
        Assert.That(builder.Length, Is.EqualTo(1));
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
        using ArrayBuilderWrapper<char> builder = CompileWrapper<char>("AddRange(System.Collections.Generic.IEnumerable<T>)");

        // Act
        builder.AddRange(values);

        // Assert
        char[] array = values.ToArray();
        Assert.That(builder.Length, Is.EqualTo(array.Length));
        for (int i = 0; i < array.Length; i++)
        {
            Assert.That(builder[i], Is.EqualTo(array[i]));
        }
    }

    [Test]
    public void AddRange_String()
    {
        // Arrange
        using ArrayBuilderWrapper<char> builder =
            CompileWrapper<char>($"{Constants.NAMESPACE}.ArrayBuilderExtensions.AddRange({Constants.ARRAY_BUILDER}<char>, string)", "ToString()");

        Type extensionsType = builder.Instance.GetType().Assembly.GetType($"{Constants.NAMESPACE}.ArrayBuilderExtensions", true)!;
        MethodInfo method = GetMethod(extensionsType, "AddRange", BindingFlags.Static | BindingFlags.Public, builder.Instance.GetType(), typeof(string));
        string message = Fake.Lorem.Sentence();

        // Act
        method.InvokeStatic(builder.Instance, message);

        // Assert
        Assert.That(builder.ToString(), Is.EqualTo(message));
    }

    [Test]
    public void Remove()
    {
        // Arrange
        using ArrayBuilderWrapper<char> builder = CompileWrapper<char>("Remove(T)", "AddRange(System.Collections.Generic.IEnumerable<T>)");
        char[] values = "abcdefg".ToCharArray();
        char toRemove = 'b';
        builder.AddRange(values);

        // Act
        bool result = builder.Remove(toRemove);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(builder.Length, Is.EqualTo(values.Length - 1));
        //TODO: Check contains
    }

    [Test]
    public void RemoveAt()
    {
        // Arrange
        using ArrayBuilderWrapper<char> builder = CompileWrapper<char>("Remove(T)", "AddRange(System.Collections.Generic.IEnumerable<T>)");
        char[] values = "abcdefg".ToCharArray();
        int toRemove = 2;
        builder.AddRange(values);

        // Act
        builder.RemoveAt(toRemove);

        // Assert
        Assert.That(builder.Length, Is.EqualTo(values.Length - 1));
        Assert.That(builder[toRemove], Is.Not.EqualTo(values[toRemove]));
        //TODO: Check contains
    }

    [Test]
    public void IndexOf_Valid()
    {
        // Arrange
        using ArrayBuilderWrapper<int> builder = CompileWrapper<int>("IndexOf(T)", "AddRange(System.Collections.Generic.IEnumerable<T>)");
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
        using ArrayBuilderWrapper<int> builder = CompileWrapper<int>("IndexOf(T)", "AddRange(System.Collections.Generic.IEnumerable<T>)");
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
        using ArrayBuilderWrapper<int> builder = CompileWrapper<int>("Contains(T)", "AddRange(System.Collections.Generic.IEnumerable<T>)");
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
        using ArrayBuilderWrapper<int> builder = CompileWrapper<int>("Contains(T)", "AddRange(System.Collections.Generic.IEnumerable<T>)");
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
        using ArrayBuilderWrapper<char> builder = CompileWrapper<char>("Clear()", "AddRange(System.Collections.Generic.IEnumerable<T>)");
        builder.AddRange(Fake.Random.Chars(count: 32));

        // Act
        builder.Clear();

        // Assert
        Assert.That(builder.Length, Is.EqualTo(0));
    }

    [Test]
    public void ToArray()
    {
        // Arrange
        using ArrayBuilderWrapper<char> builder = CompileWrapper<char>("ToArray()", "AddRange(System.Collections.Generic.IEnumerable<T>)");
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
        using ArrayBuilderWrapper<char> builder = CompileWrapper<char>("ToImmutableArray()", "AddRange(System.Collections.Generic.IEnumerable<T>)");
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
        using ArrayBuilderWrapper<char> builder = CompileWrapper<char>("ToString()", "AddRange(System.Collections.Generic.IEnumerable<T>)");
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
        using ArrayBuilderWrapper<byte> builder = CompileWrapper<byte>("ToString()", "AddRange(System.Collections.Generic.IEnumerable<T>)");
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
        ArrayBuilderWrapper<byte> builder = CompileWrapper<byte>("AddRange(System.Collections.Generic.IEnumerable<T>)");
        builder.AddRange(Fake.Random.Bytes(32));
        byte[] internalArray = builder.InternalArray;

        // Act
        builder.Dispose();

        // Assert
        for (int i = 0; i < 32; i++)
        {
            Assert.That(internalArray[i], Is.EqualTo(0));
        }

        Assert.That(builder.Length, Is.EqualTo(0));
        Assert.That(builder.InternalArray, Is.Empty);
    }

    [Test]
    public void Indexer_Get()
    {
        // Arrange
        using ArrayBuilderWrapper<int> builder = CompileWrapper<int>("Add(T)");
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
        using ArrayBuilderWrapper<int> builder = CompileWrapper<int>("Add(T)");
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
        ArrayBuilderWrapper<int> builder = CompileWrapper<int>("Add(T)");
        int value = Fake.Random.Int();
        builder.Add(value);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            int i = builder[index];
        });
    }

    [Test]
    [TestCase(-1)]
    [TestCase(1)]
    [TestCase(10)]
    public void Indexer_Set_OutOfRange_ThrowsException(int index)
    {
        // Arrange
        ArrayBuilderWrapper<int> builder = CompileWrapper<int>("Add(T)");
        int value = Fake.Random.Int();
        builder.Add(value);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => { builder[index] = 420; });
    }

    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "ArrayBuilder";
    }

    public static ArrayBuilderWrapper<T> CompileWrapper<T>(params string[] calledMethods)
    {
        Type type = CompileGeneratedType("ArrayBuilder`1", AppendTypeIfNeeded("ArrayBuilder", [.. calledMethods, "ArrayBuilder()"]));

        object instance = CreateInstance(type.MakeGenericType(typeof(T)));
        return new ArrayBuilderWrapper<T>(instance);
    }

    public readonly struct ArrayBuilderWrapper<T> : IDisposable
    {
        public readonly object Instance;
        private readonly PropertyInfo lengthProperty;
        private readonly PropertyInfo indexer;
        private readonly MethodInfo addMethod;
        private readonly MethodInfo addRangeIEnumerable;
        private readonly MethodInfo removeMethod;
        private readonly MethodInfo removeAtMethod;
        private readonly MethodInfo indexOfMethod;
        private readonly MethodInfo containsMethod;
        private readonly MethodInfo clearMethod;
        private readonly MethodInfo disposeMethod;
        private readonly MethodInfo toArrayMethod;
        private readonly MethodInfo toImmutableArrayMethod;
        private readonly MethodInfo toString;
        private readonly FieldInfo internalArray;
        private readonly FieldInfo writerField;

        public int Length
        {
            get { return (int) lengthProperty.GetValue(Instance)!; }
        }

        public T[] InternalArray
        {
            get { return (T[]) internalArray.GetValue(writerField.GetValue(Instance))!; }
        }

        public ArrayBuilderWrapper(object instance)
        {
            Instance = instance;
            Type type = instance.GetType();

            lengthProperty = GetProperty(type, "Count", BindingFlags.Public | BindingFlags.Instance)!;
            indexer = GetProperty(type, "Item", BindingFlags.Public | BindingFlags.Instance)!;
            addMethod = GetMethod(type, "Add", BindingFlags.Public | BindingFlags.Instance);
            addRangeIEnumerable = GetMethod(type, "AddRange", BindingFlags.Public | BindingFlags.Instance, typeof(IEnumerable<T>));
            removeMethod = GetMethod(type, "Remove", BindingFlags.Public | BindingFlags.Instance);
            removeAtMethod = GetMethod(type, "RemoveAt", BindingFlags.Public | BindingFlags.Instance);
            indexOfMethod = GetMethod(type, "IndexOf", BindingFlags.Public | BindingFlags.Instance);
            containsMethod = GetMethod(type, "Contains", BindingFlags.Public | BindingFlags.Instance);
            clearMethod = GetMethod(type, "Clear", BindingFlags.Public | BindingFlags.Instance);
            disposeMethod = GetMethod(type, "Dispose", BindingFlags.Public | BindingFlags.Instance);
            toArrayMethod = GetMethod(type, "ToArray", BindingFlags.Public | BindingFlags.Instance);
            toImmutableArrayMethod = GetMethod(type, "ToImmutableArray", BindingFlags.Public | BindingFlags.Instance);
            toString = GetMethod(type, "ToString", BindingFlags.Public | BindingFlags.Instance);
            writerField = GetField(type, "writer", BindingFlags.Instance | BindingFlags.NonPublic);

            Type writerType = writerField.FieldType;
            internalArray = GetField(writerType!, "array", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public T this[int index]
        {
            get { return (T) indexer.GetMethod!.InvokeInstance(Instance, index)!; }
            set { indexer.SetMethod!.InvokeInstance(Instance, index, value); }
        }

        public void Add(T value)
        {
            addMethod.InvokeInstance(Instance, value);
        }

        public void AddRange(IEnumerable<T> values)
        {
            addRangeIEnumerable.InvokeInstance(Instance, values);
        }

        public bool Remove(T value)
        {
            return removeMethod.InvokeInstance<bool>(Instance, value);
        }

        public void RemoveAt(int index)
        {
            removeAtMethod.InvokeInstance(Instance, index);
        }

        public int IndexOf(T value)
        {
            return (int) indexOfMethod.InvokeInstance(Instance, value)!;
        }

        public bool Contains(T value)
        {
            return (bool) containsMethod.InvokeInstance(Instance, value)!;
        }

        public void Clear()
        {
            clearMethod.InvokeInstance(Instance);
        }

        public T[] ToArray()
        {
            return (T[]) toArrayMethod.InvokeInstance(Instance)!;
        }

        public ImmutableArray<T> ToImmutableArray()
        {
            return (ImmutableArray<T>) toImmutableArrayMethod.InvokeInstance(Instance)!;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return (string) toString.InvokeInstance(Instance)!;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            disposeMethod.InvokeInstance(Instance);
        }
    }
}