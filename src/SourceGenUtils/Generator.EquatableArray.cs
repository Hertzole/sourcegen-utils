using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private static TypeSource CreateEquatableArray()
    {
        const string equatable_array = NAMESPACE + ".EquatableArray";

        return new TypeSource
        {
            Signature =
                "internal readonly partial struct EquatableArray<T> : global::System.IEquatable<EquatableArray<T>>, global::System.Collections.Generic.IEnumerable<T> where T : global::System.IEquatable<T>",
            Fields = new Dictionary<string, FieldSource>
            {
                ["array"] = new FieldSource
                {
                    Signature = "private readonly T[]? array;"
                }
            },
            Properties = new Dictionary<string, PropertySource>
            {
                ["IsEmpty"] = new PropertySource
                {
                    Signature = "public bool IsEmpty",
                    GetAttributes = AggressiveInlineAttribute,
                    GetImplementation = (writer, in _) => { writer.AppendLine("return array == null || array.Length == 0;"); }
                },
                ["Length"] = new PropertySource
                {
                    Signature = "public int Length",
                    GetAttributes = AggressiveInlineAttribute,
                    GetImplementation = (writer, in _) => { writer.AppendLine("return array == null ? 0 : array.Length;"); }
                },
                ["this"] = new PropertySource
                {
                    Signature = "public ref readonly T this[int index]",
                    GetAttributes = AggressiveInlineAttribute,
                    GetImplementation = (writer, in _) => { writer.AppendLine("return ref AsImmutableArray().ItemRef(index);"); }
                }
            },
            Methods =
            [
                new MethodSource
                {
                    Name = "EquatableArray",
                    Signature = "public partial EquatableArray(T[] array)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("this.array = new T[array.Length];");
                        writer.AppendLine("global::System.Array.Copy(array, this.array, array.Length);");
                    }
                },
                new MethodSource
                {
                    Name = "EquatableArray",
                    Signature = "public partial EquatableArray(global::System.Collections.Immutable.ImmutableArray<T> array)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(
                            "this.array = global::System.Runtime.CompilerServices.Unsafe.As<global::System.Collections.Immutable.ImmutableArray<T>, T[]?>(ref array);");
                    }
                },
                new MethodSource
                {
                    Name = "AsImmutableArray",
                    Signature = "public partial global::System.Collections.Immutable.ImmutableArray<T> AsImmutableArray()",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(
                            "return global::System.Runtime.CompilerServices.Unsafe.As<T[]?, global::System.Collections.Immutable.ImmutableArray<T>>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef(in array));");
                    },
                    AlwaysWrite = true
                },
                new MethodSource
                {
                    Name = "AsSpan",
                    Signature = "public partial global::System.ReadOnlySpan<T> AsSpan()",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(
                            "return array == null ? global::System.ReadOnlySpan<T>.Empty : new global::System.ReadOnlySpan<T>(array, 0, array.Length);");
                    },
                    EmptyStub = "return global::System.ReadOnlySpan<T>.Empty;",
                    AlwaysWrite = true
                },
                new MethodSource
                {
                    Name = "GetEnumerator",
                    Signature = "public partial global::System.Collections.Immutable.ImmutableArray<T>.Enumerator GetEnumerator()",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) => { writer.AppendLine("return AsImmutableArray().GetEnumerator();"); },
                    AlwaysWrite = true
                },
                new MethodSource
                {
                    Name = "GetEnumerator",
                    Signature = "global::System.Collections.Generic.IEnumerator<T> global::System.Collections.Generic.IEnumerable<T>.GetEnumerator()",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("return ((global::System.Collections.Generic.IEnumerable<T>) AsImmutableArray()).GetEnumerator();");
                    },
                    AlwaysWrite = true,
                    SkipPartial = true
                },
                new MethodSource
                {
                    Name = "GetEnumerator",
                    Signature = "global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("return ((global::System.Collections.IEnumerable) AsImmutableArray()).GetEnumerator();");
                    },
                    AlwaysWrite = true,
                    SkipPartial = true
                },
                new MethodSource
                {
                    Name = "Equals",
                    Signature = "public partial bool Equals(global::" + equatable_array + "<T> other)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("return global::System.MemoryExtensions.SequenceEqual<T>(AsSpan(), other.AsSpan());");
                    },
                    AlwaysWrite = true
                },
                new MethodSource
                {
                    Name = "Equals",
                    Signature = "public override partial bool Equals(object? other)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("return other is global::" + equatable_array + "<T> array && Equals(this, array);");
                    },
                    AlwaysWrite = true
                },
                new MethodSource
                {
                    Name = "GetHashCode",
                    Signature = "public override partial int GetHashCode()",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("if (array == null)");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("return 0;");
                        }

                        writer.AppendLine("int hash = 17;");
                        writer.AppendLine("unchecked");
                        using (writer.WithBlock())
                        {
                            writer.AppendLine("for (int i = 0; i < Length; i++)");
                            using (writer.WithBlock())
                            {
                                writer.AppendLine("hash = hash * 31 + (this[i]?.GetHashCode() ?? 0);");
                            }
                        }

                        writer.AppendLine("return hash;");
                    },
                    AlwaysWrite = true
                },
                new MethodSource
                {
                    Name = "EquatableArrayOperator",
                    Signature = "public static implicit operator global::" + equatable_array +
                                "<T>(global::System.Collections.Immutable.ImmutableArray<T> array)",
                    Implementation = (writer, in _) => { writer.AppendLine("return new global::" + equatable_array + "<T>(array);"); },
                    AlwaysWrite = true,
                    SkipPartial = true
                },
                new MethodSource
                {
                    Name = "ImmutableArrayOperator",
                    Signature = "public static implicit operator global::System.Collections.Immutable.ImmutableArray<T>(global::" + equatable_array +
                                "<T> array)",
                    Implementation = (writer, in _) => { writer.AppendLine("return array.AsImmutableArray();"); },
                    AlwaysWrite = true,
                    SkipPartial = true
                },
                new MethodSource
                {
                    Name = "==",
                    Signature = "public static bool operator ==(global::" + equatable_array + "<T> left, global::" + equatable_array + "<T> right)",
                    Implementation = (writer, in _) => { writer.AppendLine("return left.Equals(right);"); },
                    AlwaysWrite = true,
                    SkipPartial = true
                },
                new MethodSource
                {
                    Name = "!=",
                    Signature = "public static bool operator !=(global::" + equatable_array + "<T> left, global::" + equatable_array + "<T> right)",
                    Implementation = (writer, in _) => { writer.AppendLine("return !left.Equals(right);"); },
                    AlwaysWrite = true,
                    SkipPartial = true
                }
            ]
        };
    }
}