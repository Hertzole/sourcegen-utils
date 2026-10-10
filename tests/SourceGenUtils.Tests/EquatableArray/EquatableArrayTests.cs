using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Hertzole.SourceGen.Wrappers;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
internal class EquatableArrayTests : GeneratorTests
{
    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "EquatableArray";
    }

    [Test]
    public void Constructor_Array_CopiesArray()
    {
        // Arrange
        Type type = CompileType("var a = new EquatableArray<int>(new int[0]);");
        int[] original = Fake.Random.Digits(32);
        int[] source = new int[original.Length];
        original.CopyTo(source, 0);
        EquatableArray<int> array = new EquatableArray<int>(type, source);

        // Act
        source[0] = 99;

        // Assert
        Assert.That(array.array, Is.EqualTo(original));
        Assert.That(array.Length, Is.EqualTo(original.Length));
    }

    [Test]
    public void Constructor_ImmutableArray()
    {
        // Arrange
        Type type = CompileType("var a = new EquatableArray<int>(System.Collections.Immutable.ImmutableArray.Create(new int[0]));");
        int[] source = Fake.Random.Digits(32);
        ImmutableArray<int> immutable = ImmutableArray.Create(source);

        // Act
        EquatableArray<int> array = new EquatableArray<int>(type, immutable);

        // Assert
        Assert.That(array.Length, Is.EqualTo(source.Length));
        Assert.That(array.array, Is.EqualTo(immutable.ToArray()));
    }

    [Test]
    public void IsEmpty()
    {
        // Arrange
        Type type = CompileType("var a = new EquatableArray<int>(new int[] { 1 }); bool empty = a.IsEmpty;");
        EquatableArray<int> empty = new EquatableArray<int>(type, Array.Empty<int>());
        EquatableArray<int> filled = new EquatableArray<int>(type, new[]
        {
            1
        });

        // Act & Assert
        Assert.That(empty.IsEmpty, Is.True);
        Assert.That(filled.IsEmpty, Is.False);
    }

    [Test]
    public void Length()
    {
        // Arrange
        Type type = CompileType("var a = new EquatableArray<int>(new int[] { 1, 2, 3 }); int length = a.Length;");
        EquatableArray<int> empty = new EquatableArray<int>(type, Array.Empty<int>());
        EquatableArray<int> filled = new EquatableArray<int>(type, new[]
        {
            1,
            2,
            3
        });

        // Act & Assert
        Assert.That(empty.Length, Is.EqualTo(0));
        Assert.That(filled.Length, Is.EqualTo(3));
    }

    [Test]
    public void Indexer_Get()
    {
        // Arrange
        Type type = CompileType("var a = new EquatableArray<int>(new int[] { 10, 20, 30 }); int item = a[1];");
        EquatableArray<int> array = new EquatableArray<int>(type, new[]
        {
            10,
            20,
            30
        });

        // Act
        int result = array[1];

        // Assert
        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void AsImmutableArray()
    {
        // Arrange
        Type type = CompileType(
            "var a = new EquatableArray<int>(new int[] { 1, 2, 3 }); System.Collections.Immutable.ImmutableArray<int> result = a.AsImmutableArray();");

        int[] values = { 1, 2, 3 };
        EquatableArray<int> array = new EquatableArray<int>(type, values);

        // Act
        ImmutableArray<int> result = array.AsImmutableArray();

        // Assert
        Assert.That(result, Is.EqualTo(values));
    }

    [Test]
    public void AsSpan()
    {
        // Arrange
        Type type = CompileType("var a = new EquatableArray<int>(new int[] { 1, 2, 3, 4, 5 }); System.ReadOnlySpan<int> result = a.AsSpan();");
        int[] values = { 1, 2, 3, 4, 5 };
        EquatableArray<int> array = new EquatableArray<int>(type, values);

        // Act
        ReadOnlySpan<int> result = array.AsSpan();

        // Assert
        Assert.That(result.ToArray(), Is.EqualTo(values));
    }

    [Test]
    public void GetEnumerator()
    {
        // Arrange
        Type type = CompileType("var a = new EquatableArray<int>(new int[] { 1, 2, 3 }); var enumerator = a.GetEnumerator();");
        int[] values = { 1, 2, 3 };
        EquatableArray<int> array = new EquatableArray<int>(type, values);

        // Act
        ImmutableArray<int>.Enumerator enumerator = array.GetEnumerator();
        List<int> result = new List<int>();
        while (enumerator.MoveNext())
        {
            result.Add(enumerator.Current);
        }

        // Assert
        Assert.That(result, Is.EqualTo(values));
    }

    [Test]
    public void GetEnumerator_GenericInterface()
    {
        // Arrange
        Type type = CompileType("var a = new EquatableArray<int>(new int[] { 1, 2, 3 }); var list = new System.Collections.Generic.List<int>(a);");
        int[] values = { 1, 2, 3 };
        EquatableArray<int> array = new EquatableArray<int>(type, values);

        // Act
        List<int> result = new List<int>((IEnumerable<int>) array.Instance);

        // Assert
        Assert.That(result, Is.EqualTo(values));
    }

    [Test]
    public void GetEnumerator_NonGeneric()
    {
        // Arrange
        Type type = CompileType(
            "var a = new EquatableArray<int>(new int[] { 1, 2, 3 }); System.Collections.IEnumerator enumerator = ((System.Collections.IEnumerable) a).GetEnumerator();");

        int[] values = { 1, 2, 3 };
        EquatableArray<int> array = new EquatableArray<int>(type, values);

        // Act
        IEnumerator enumerator = ((IEnumerable) array.Instance).GetEnumerator();
        List<int> result = new List<int>();
        while (enumerator.MoveNext())
        {
            result.Add((int) enumerator.Current!);
        }

        // Assert
        Assert.That(result, Is.EqualTo(values));
    }

    [Test]
    public void Equals_EqualArrays_ReturnsTrue()
    {
        // Arrange
        Type type = CompileType(
            "var a = new EquatableArray<int>(new int[] { 1, 2, 3 }); var b = new EquatableArray<int>(new int[] { 1, 2, 3 }); bool equals = a.Equals(b);");

        EquatableArray<int> first = new EquatableArray<int>(type, new[]
        {
            1,
            2,
            3
        });

        EquatableArray<int> second = new EquatableArray<int>(type, new[]
        {
            1,
            2,
            3
        });

        // Act
        bool result = first.Equals(second);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Equals_DifferentArrays_ReturnsFalse()
    {
        // Arrange
        Type type = CompileType(
            "var a = new EquatableArray<int>(new int[] { 1, 2, 3 }); var b = new EquatableArray<int>(new int[] { 1, 2, 4 }); bool equals = a.Equals(b);");

        EquatableArray<int> first = new EquatableArray<int>(type, new[]
        {
            1,
            2,
            3
        });

        EquatableArray<int> second = new EquatableArray<int>(type, new[]
        {
            1,
            2,
            4
        });

        // Act
        bool result = first.Equals(second);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Equals_Object_EqualArray_ReturnsTrue()
    {
        // Arrange
        Type type = CompileType(
            "var a = new EquatableArray<int>(new int[] { 1, 2, 3 }); var b = new EquatableArray<int>(new int[] { 1, 2, 3 }); bool equals = a.Equals((object) b);");

        EquatableArray<int> first = new EquatableArray<int>(type, new[]
        {
            1,
            2,
            3
        });

        EquatableArray<int> second = new EquatableArray<int>(type, new[]
        {
            1,
            2,
            3
        });

        // Act
        bool result = first.Equals(second.Instance);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Equals_Object_NotEquatableArray_ReturnsFalse()
    {
        // Arrange
        Type type = CompileType("var a = new EquatableArray<int>(new int[] { 1, 2, 3 }); bool equals = a.Equals((object) new object());");
        EquatableArray<int> array = new EquatableArray<int>(type, new[]
        {
            1,
            2,
            3
        });

        // Act
        bool result = array.Equals(new object());

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Equals_Object_Null_ReturnsFalse()
    {
        // Arrange
        Type type = CompileType("var a = new EquatableArray<int>(new int[] { 1, 2, 3 }); bool equals = a.Equals(null);");
        EquatableArray<int> array = new EquatableArray<int>(type, new[]
        {
            1,
            2,
            3
        });

        // Act
        bool result = array.Equals(null);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void GetHashCode_EqualArrays_ReturnsSameHash()
    {
        // Arrange
        Type type = CompileType("var a = new EquatableArray<int>(new int[] { 1, 2, 3 }); int hashCode = a.GetHashCode();");
        EquatableArray<int> first = new EquatableArray<int>(type, new[]
        {
            1,
            2,
            3
        });

        EquatableArray<int> second = new EquatableArray<int>(type, new[]
        {
            1,
            2,
            3
        });

        // Act
        int firstHash = first.GetHashCode();
        int secondHash = second.GetHashCode();

        // Assert
        Assert.That(firstHash, Is.EqualTo(secondHash));
    }

    private static Type CompileType(string usage)
    {
        return CompileGeneratedTypeByUsing("EquatableArray`1", usage).MakeGenericType(typeof(int));
    }
}