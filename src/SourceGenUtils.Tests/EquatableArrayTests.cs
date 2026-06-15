using System.Collections.Generic;
using System.Threading;
using Hertzole.SourceGenUtils;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

internal class EquatableArrayTests : GeneratorTests
{
    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "EquatableArray";
    }

    /// <inheritdoc />
    protected override string GetTypeOutline()
    {
        return """
               internal readonly partial struct EquatableArray<T> : global::System.IEquatable<EquatableArray<T>>, global::System.Collections.Generic.IEnumerable<T> where T : global::System.IEquatable<T>
               {
               }
               """;
    }

    /// <inheritdoc />
    protected override string[] GetShellMethods()
    {
        return
        [
            "EquatableArray(T[])",
            "EquatableArray(System.Collections.Immutable.ImmutableArray<T>)",
            "AsImmutableArray()",
            "AsSpan()",
            "GetEnumerator()",
            "Equals(Hertzole.SourceGen.EquatableArray<T>)",
            "Equals(object?)",
            "GetHashCode()"
        ];
    }

    [Test]
    public void Call_EquatableArray_Array()
    {
        string noCalls = GetTypeContent();
        string called = GetTypeContent("EquatableArray.EquatableArray(T[])");

        Assert.That(called, Is.Not.EqualTo(noCalls), "Constructor was not detected as called.");
    }

    [Test]
    public void Call_EquatableArray_ImmutableArray()
    {
        string noCalls = GetTypeContent();
        string called = GetTypeContent("EquatableArray.EquatableArray(System.Collections.Immutable.ImmutableArray<T>)");

        Assert.That(called, Is.Not.EqualTo(noCalls), "ImmutableArray constructor was not detected as called.");
    }

    [Test]
    public void EquatableArray_Array_Content()
    {
        // Arrange
        string content = GetMethodContent("EquatableArray.EquatableArray(T[])");
        const string expected = """
                                public partial EquatableArray(T[] array)
                                {
                                    this.array = new T[array.Length];
                                    global::System.Array.Copy(array, this.array, array.Length);
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void EquatableArray_ImmutableArray_Content()
    {
        // Arrange
        string content = GetMethodContent("EquatableArray.EquatableArray(System.Collections.Immutable.ImmutableArray<T>)");
        const string expected = """
                                public partial EquatableArray(global::System.Collections.Immutable.ImmutableArray<T> array)
                                {
                                    this.array = global::System.Runtime.CompilerServices.Unsafe.As<global::System.Collections.Immutable.ImmutableArray<T>, T[]?>(ref array);
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AsImmutableArray_Content()
    {
        // Arrange
        string content = GetMethodContent("EquatableArray.AsImmutableArray()");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::System.Collections.Immutable.ImmutableArray<T> AsImmutableArray()
                                {
                                    return global::System.Runtime.CompilerServices.Unsafe.As<T[]?, global::System.Collections.Immutable.ImmutableArray<T>>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef(in array));
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void AsSpan_Content()
    {
        // Arrange
        string content = GetMethodContent("EquatableArray.AsSpan()");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::System.ReadOnlySpan<T> AsSpan()
                                {
                                    return array == null ? global::System.ReadOnlySpan<T>.Empty : new global::System.ReadOnlySpan<T>(array, 0, array.Length);
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void GetEnumerator_Content()
    {
        // Arrange
        string content = GetMethodContent("EquatableArray.GetEnumerator()");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::System.Collections.Immutable.ImmutableArray<T>.Enumerator GetEnumerator()
                                {
                                    return AsImmutableArray().GetEnumerator();
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void GetEnumerator_IEnumeratorT_Content()
    {
        // Arrange
        string content = GetMethodContentByIndex(5);
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                global::System.Collections.Generic.IEnumerator<T> global::System.Collections.Generic.IEnumerable<T>.GetEnumerator()
                                {
                                    return ((global::System.Collections.Generic.IEnumerable<T>) AsImmutableArray()).GetEnumerator();
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void GetEnumerator_IEnumerator_Content()
    {
        // Arrange
        string content = GetMethodContentByIndex(6);
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
                                {
                                    return ((global::System.Collections.IEnumerable) AsImmutableArray()).GetEnumerator();
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Equals_EquatableArray_Content()
    {
        // Arrange
        string content = GetMethodContent("EquatableArray.Equals(Hertzole.SourceGen.EquatableArray<T>)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial bool Equals(global::Hertzole.SourceGen.EquatableArray<T> other)
                                {
                                    return global::System.MemoryExtensions.SequenceEqual<T>(AsSpan(), other.AsSpan());
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Equals_Object_Content()
    {
        // Arrange
        string content = GetMethodContent("EquatableArray.Equals(object?)");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public override partial bool Equals(object? other)
                                {
                                    return other is global::Hertzole.SourceGen.EquatableArray<T> array && Equals(this, array);
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void GetHashCode_Content()
    {
        // Arrange
        string content = GetMethodContent("EquatableArray.GetHashCode()");
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public override partial int GetHashCode()
                                {
                                    if (array == null)
                                    {
                                        return 0;
                                    }
                                    int hash = 17;
                                    unchecked
                                    {
                                        for (int i = 0; i < Length; i++)
                                        {
                                            hash = hash * 31 + (this[i]?.GetHashCode() ?? 0);
                                        }
                                    }
                                    return hash;
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void EquatableArrayOperator_Content()
    {
        // Arrange
        string content = GetMethodContent("EquatableArray.EquatableArrayOperator(System.Collections.Immutable.ImmutableArray<T>)");
        const string expected = """
                                public static implicit operator global::Hertzole.SourceGen.EquatableArray<T>(global::System.Collections.Immutable.ImmutableArray<T> array)
                                {
                                    return new global::Hertzole.SourceGen.EquatableArray<T>(array);
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void ImmutableArrayOperator_Content()
    {
        // Arrange
        string content = GetMethodContent("EquatableArray.ImmutableArrayOperator(Hertzole.SourceGen.EquatableArray<T>)");
        const string expected = """
                                public static implicit operator global::System.Collections.Immutable.ImmutableArray<T>(global::Hertzole.SourceGen.EquatableArray<T> array)
                                {
                                    return array.AsImmutableArray();
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Operator_Equality_Content()
    {
        // Arrange
        string content = GetMethodContent("EquatableArray.==(Hertzole.SourceGen.EquatableArray<T>, Hertzole.SourceGen.EquatableArray<T>)");
        const string expected = """
                                public static bool operator ==(global::Hertzole.SourceGen.EquatableArray<T> left, global::Hertzole.SourceGen.EquatableArray<T> right)
                                {
                                    return left.Equals(right);
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Operator_Inequality_Content()
    {
        // Arrange
        string content = GetMethodContent("EquatableArray.!=(Hertzole.SourceGen.EquatableArray<T>, Hertzole.SourceGen.EquatableArray<T>)");
        const string expected = """
                                public static bool operator !=(global::Hertzole.SourceGen.EquatableArray<T> left, global::Hertzole.SourceGen.EquatableArray<T> right)
                                {
                                    return !left.Equals(right);
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void EquatableArray_Array_Content_NotCalled()
    {
        // Arrange
        const string expected = """
                                public partial EquatableArray(T[] array)
                                {
                                }
                                """;

        // Assert
        EmptyContentTest("EquatableArray.EquatableArray(T[])", expected);
    }

    [Test]
    public void EquatableArray_ImmutableArray_Content_NotCalled()
    {
        // Arrange
        const string expected = """
                                public partial EquatableArray(global::System.Collections.Immutable.ImmutableArray<T> array)
                                {
                                }
                                """;

        // Assert
        EmptyContentTest("EquatableArray.EquatableArray(System.Collections.Immutable.ImmutableArray<T>)", expected);
    }

    [Test]
    public void AsImmutableArray_Content_NotCalled()
    {
        // Arrange
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::System.Collections.Immutable.ImmutableArray<T> AsImmutableArray()
                                {
                                    return global::System.Runtime.CompilerServices.Unsafe.As<T[]?, global::System.Collections.Immutable.ImmutableArray<T>>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef(in array));
                                }
                                """;

        // Assert
        EmptyContentTest("EquatableArray.AsImmutableArray()", expected);
    }

    [Test]
    public void AsSpan_Content_NotCalled()
    {
        // Arrange
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::System.ReadOnlySpan<T> AsSpan()
                                {
                                    return array == null ? global::System.ReadOnlySpan<T>.Empty : new global::System.ReadOnlySpan<T>(array, 0, array.Length);
                                }
                                """;

        // Assert
        EmptyContentTest("EquatableArray.AsSpan()", expected);
    }

    [Test]
    public void GetEnumerator_Content_NotCalled()
    {
        // Arrange
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial global::System.Collections.Immutable.ImmutableArray<T>.Enumerator GetEnumerator()
                                {
                                    return AsImmutableArray().GetEnumerator();
                                }
                                """;

        // Assert
        EmptyContentTest("EquatableArray.GetEnumerator()", expected);
    }

    [Test]
    public void Equals_EquatableArray_Content_NotCalled()
    {
        // Arrange
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public partial bool Equals(global::Hertzole.SourceGen.EquatableArray<T> other)
                                {
                                    return global::System.MemoryExtensions.SequenceEqual<T>(AsSpan(), other.AsSpan());
                                }
                                """;

        // Assert
        EmptyContentTest("EquatableArray.Equals(Hertzole.SourceGen.EquatableArray<T>)", expected);
    }

    [Test]
    public void Equals_Object_Content_NotCalled()
    {
        // Arrange
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public override partial bool Equals(object? other)
                                {
                                    return other is global::Hertzole.SourceGen.EquatableArray<T> array && Equals(this, array);
                                }
                                """;

        // Assert
        EmptyContentTest("EquatableArray.Equals(object?)", expected);
    }

    [Test]
    public void GetHashCode_Content_NotCalled()
    {
        // Arrange
        const string expected = """
                                [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                                public override partial int GetHashCode()
                                {
                                    if (array == null)
                                    {
                                        return 0;
                                    }
                                    int hash = 17;
                                    unchecked
                                    {
                                        for (int i = 0; i < Length; i++)
                                        {
                                            hash = hash * 31 + (this[i]?.GetHashCode() ?? 0);
                                        }
                                    }
                                    return hash;
                                }
                                """;

        // Assert
        EmptyContentTest("EquatableArray.GetHashCode()", expected);
    }

    [Test]
    public void Field_Array_Content()
    {
        // Arrange
        string content = GetFieldContent("EquatableArray.array");
        const string expected = """
                                private readonly T[]? array;
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    private static string GetMethodContentByIndex(int methodIndex)
    {
        CancellationToken cancellationToken = CancellationToken.None;
        MethodSource method = Generator.TypesToGenerate["EquatableArray"].Methods![methodIndex];
        string fullName = $"Hertzole.SourceGen.EquatableArray.{method.Name}({method.ParameterTypesKey})";
        CodeWriter writer = new CodeWriter();
        Generator.AppendMethod(writer, method, fullName, new ImplementationContext(new HashSet<string>(), cancellationToken));
        return writer.ToString();
    }
}