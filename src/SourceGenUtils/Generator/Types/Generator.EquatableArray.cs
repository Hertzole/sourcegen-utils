using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private const string EQUATABLE_ARRAY_EXAMPLE =
        """
        The following example uses EquatableArray in a model passed through an incremental generator pipeline so that changes are detected by value instead of by reference.

        <code>
        using System.Collections.Immutable;
        using Hertzole.SourceGen;

        internal readonly record struct TypeModel(string Name, EquatableArray<string> Members);

        internal static class ModelFactory
        {
            public static TypeModel Create(string name, ImmutableArray<string> members)
            {
                // Implicitly converts ImmutableArray<T> to EquatableArray<T>.
                return new TypeModel(name, members);
            }

            public static bool HasSameMembers(TypeModel left, TypeModel right)
            {
                // Compares the contents of the arrays, not the array references.
                return left.Members.Equals(right.Members);
            }
        }
        </code>
        """;

    private static TypeSource CreateEquatableArray()
    {
        const string equatable = "global::System.IEquatable";
        string arrayTrivia = GetTypeTriviaReference($"{GLOBAL_EQUATABLE_ARRAY}{{T}}", "EquatableArray<T>");
        string immutableTrivia = GetTypeTriviaReference("global::System.Collections.Immutable.ImmutableArray{T}", "ImmutableArray<T>");

        return new TypeSource
        {
            Signature =
                $"internal readonly partial struct EquatableArray<T> : {equatable}<EquatableArray<T>>, global::System.Collections.Generic.IEnumerable<T> where T : {equatable}<T>",
            Trivia = new TriviaSource
            {
                Summary =
                    $"A wrapper around an array that implements {GetTypeTriviaReference($"{equatable}{{T}}", "IEquatable<T>")} for value-based equality comparison.",
                Example = EQUATABLE_ARRAY_EXAMPLE
            },
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
                    },
                    AlwaysWrite = true,
                    Trivia = new TriviaSource
                    {
                        Summary =
                            $"Creates a new {arrayTrivia} from the specified array. The array is copied.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["array"] = "The array to wrap."
                        }
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
                    },
                    AlwaysWrite = true,
                    Trivia = new TriviaSource
                    {
                        Summary =
                            $"Creates a new {arrayTrivia} from the specified immutable array.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["array"] = "The immutable array to wrap."
                        }
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
                    AlwaysWrite = true,
                    Trivia = new TriviaSource
                    {
                        Summary = $"Converts this {arrayTrivia} to an {immutableTrivia}.",
                        Returns = $"An {immutableTrivia} containing the elements."
                    }
                },
                new MethodSource
                {
                    Name = "AsSpan",
                    Signature = "public partial global::System.ReadOnlySpan<T> AsSpan()",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine(
                            $"return array == null ? {GLOBAL_R_SPAN}<T>.Empty : new {GLOBAL_R_SPAN}<T>(array, 0, array.Length);");
                    },
                    AlwaysWrite = true,
                    Trivia = new TriviaSource
                    {
                        Summary = $"Returns a read-only span over the elements of this {arrayTrivia}.",
                        Returns = "A read-only span over the elements."
                    }
                },
                new MethodSource
                {
                    Name = "GetEnumerator",
                    Signature = "public partial global::System.Collections.Immutable.ImmutableArray<T>.Enumerator GetEnumerator()",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) => { writer.AppendLine("return AsImmutableArray().GetEnumerator();"); },
                    AlwaysWrite = true,
                    Trivia = new TriviaSource
                    {
                        Summary = "Returns an enumerator that iterates through the elements.",
                        Returns = "An enumerator that iterates through the elements."
                    }
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
                    Signature = $"public partial bool Equals({GLOBAL_EQUATABLE_ARRAY}<T> other)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) => { writer.AppendLine($"return {GLOBAL_MEMORY_EXT}.SequenceEqual<T>(AsSpan(), other.AsSpan());"); },
                    AlwaysWrite = true,
                    Trivia = new TriviaSource
                    {
                        Summary =
                            $"Determines whether this instance is equal to another {arrayTrivia}.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["other"] = "The other instance to compare."
                        },
                        Returns = $"{TRIVIA_TRUE} if this instance is equal to <paramref name=\"other\"/>; otherwise {TRIVIA_FALSE}."
                    }
                },
                new MethodSource
                {
                    Name = "Equals",
                    Signature = "public override partial bool Equals(object? other)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in _) => { writer.AppendLine($"return other is {GLOBAL_EQUATABLE_ARRAY}<T> array && array.Equals(this);"); },
                    AlwaysWrite = true,
                    Trivia = new TriviaSource
                    {
                        Summary = "Determines whether this instance is equal to the specified object.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["other"] = "The object to compare."
                        },
                        Returns = $"{TRIVIA_TRUE} if this instance is equal to <paramref name=\"other\"/>; otherwise {TRIVIA_FALSE}."
                    }
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
                    AlwaysWrite = true,
                    Trivia = new TriviaSource
                    {
                        Summary = "Returns the hash code for this instance.",
                        Returns = "The hash code for this instance."
                    }
                },
                new MethodSource
                {
                    Name = "EquatableArrayOperator",
                    Signature = $"public static implicit operator {GLOBAL_EQUATABLE_ARRAY}<T>(global::System.Collections.Immutable.ImmutableArray<T> array)",
                    Implementation = (writer, in _) => { writer.AppendLine($"return new {GLOBAL_EQUATABLE_ARRAY}<T>(array);"); },
                    AlwaysWrite = true,
                    SkipPartial = true,
                    Trivia = new TriviaSource
                    {
                        Summary = $"Implicitly converts an {immutableTrivia} to an {arrayTrivia}.",
                        Returns = $"The converted {arrayTrivia}."
                    }
                },
                new MethodSource
                {
                    Name = "ImmutableArrayOperator",
                    Signature = $"public static implicit operator global::System.Collections.Immutable.ImmutableArray<T>({GLOBAL_EQUATABLE_ARRAY}<T> array)",
                    Implementation = (writer, in _) => { writer.AppendLine("return array.AsImmutableArray();"); },
                    AlwaysWrite = true,
                    SkipPartial = true,
                    Trivia = new TriviaSource
                    {
                        Summary = $"Implicitly converts an {arrayTrivia} to an {immutableTrivia}.",
                        Returns = $"The converted {immutableTrivia}."
                    }
                },
                new MethodSource
                {
                    Name = "==",
                    Signature = $"public static bool operator ==({GLOBAL_EQUATABLE_ARRAY}<T> left, {GLOBAL_EQUATABLE_ARRAY}<T> right)",
                    Implementation = (writer, in _) => { writer.AppendLine("return left.Equals(right);"); },
                    AlwaysWrite = true,
                    SkipPartial = true,
                    Trivia = new TriviaSource
                    {
                        Summary = $"Determines whether two {arrayTrivia} instances are equal.",
                        Returns = $"{TRIVIA_TRUE} if <paramref name=\"left\"/> is equal to <paramref name=\"right\"/>; otherwise {TRIVIA_FALSE}."
                    }
                },
                new MethodSource
                {
                    Name = "!=",
                    Signature = $"public static bool operator !=({GLOBAL_EQUATABLE_ARRAY}<T> left, {GLOBAL_EQUATABLE_ARRAY}<T> right)",
                    Implementation = (writer, in _) => { writer.AppendLine("return !left.Equals(right);"); },
                    AlwaysWrite = true,
                    SkipPartial = true,
                    Trivia = new TriviaSource
                    {
                        Summary = $"Determines whether two {arrayTrivia} instances are not equal.",
                        Returns = $"{TRIVIA_TRUE} if <paramref name=\"left\"/> is not equal to <paramref name=\"right\"/>; otherwise {TRIVIA_FALSE}."
                    }
                }
            ]
        };
    }
}