using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private static readonly string[] equatableArrayConstructors =
    [
        EQUATABLE_ARRAY + ".EquatableArray(T[])",
        EQUATABLE_ARRAY + ".EquatableArray(System.Collections.Immutable.ImmutableArray<T>)"
    ];

    private static TypeSource CreateEquatableArray()
    {
        const string equatable = "global::System.IEquatable";

        return new TypeSource
        {
            Signature =
                $"internal readonly partial struct EquatableArray<T> : {equatable}<EquatableArray<T>>, global::System.Collections.Generic.IEnumerable<T> where T : {equatable}<T>",
            Trivia = new TriviaSource
            {
                Summary =
                    $"A wrapper around an array that implements {GetTypeTriviaReference($"{equatable}{{T}}", "IEquatable<T>")} for value-based equality comparison."
            },
            Fields = new Dictionary<string, FieldSource>
            {
                ["array"] = new FieldSource
                {
                    Signature = "private readonly T[]? array;",
                    Dependencies = equatableArrayConstructors
                }
            },
            Properties = new Dictionary<string, PropertySource>
            {
                ["IsEmpty"] = new PropertySource
                {
                    Signature = "public bool IsEmpty",
                    GetAttributes = AggressiveInlineAttribute,
                    GetImplementation = (writer, in _) => { writer.AppendLine("return array == null || array.Length == 0;"); },
                    Dependencies = equatableArrayConstructors
                },
                ["Length"] = new PropertySource
                {
                    Signature = "public int Length",
                    GetAttributes = AggressiveInlineAttribute,
                    GetImplementation = (writer, in _) => { writer.AppendLine("return array == null ? 0 : array.Length;"); },
                    Dependencies = equatableArrayConstructors
                },
                ["this"] = new PropertySource
                {
                    Signature = "public ref readonly T this[int index]",
                    GetAttributes = AggressiveInlineAttribute,
                    GetImplementation = (writer, in _) => { writer.AppendLine("return ref AsImmutableArray().ItemRef(index);"); },
                    Dependencies = equatableArrayConstructors
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
                    Trivia = new TriviaSource
                    {
                        Summary =
                            $"Creates a new {GetTypeTriviaReference($"{EQUATABLE_ARRAY}{{T}}", "EquatableArray<T>")} from the specified array. The array is copied.",
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
                    Trivia = new TriviaSource
                    {
                        Summary = $"Creates a new {GetTypeTriviaReference($"{EQUATABLE_ARRAY}{{T}}", "EquatableArray<T>")} from the specified immutable array.",
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
                    Implementation = (writer, in context) =>
                    {
                        if (!HasConstructed(in context))
                        {
                            writer.AppendLine("return default;");
                            return;
                        }

                        writer.AppendLine(
                            "return global::System.Runtime.CompilerServices.Unsafe.As<T[]?, global::System.Collections.Immutable.ImmutableArray<T>>(ref global::System.Runtime.CompilerServices.Unsafe.AsRef(in array));");
                    },
                    AlwaysWrite = true,
                    Trivia = new TriviaSource
                    {
                        Summary =
                            $"Converts this {GetTypeTriviaReference($"{EQUATABLE_ARRAY}{{T}}", "EquatableArray<T>")} to an " +
                            $"{GetTypeTriviaReference("global::System.Collections.Immutable.ImmutableArray{T}", "ImmutableArray<T>")}.",
                        Returns =
                            $"An {GetTypeTriviaReference("global::System.Collections.Immutable.ImmutableArray{T}", "ImmutableArray<T>")} containing the elements."
                    }
                },
                new MethodSource
                {
                    Name = "AsSpan",
                    Signature = "public partial global::System.ReadOnlySpan<T> AsSpan()",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in context) =>
                    {
                        if (!HasConstructed(in context))
                        {
                            writer.AppendLine("return default;");
                            return;
                        }

                        writer.AppendLine(
                            $"return array == null ? {GLOBAL_R_SPAN}<T>.Empty : new {GLOBAL_R_SPAN}<T>(array, 0, array.Length);");
                    },
                    AlwaysWrite = true,
                    Trivia = new TriviaSource
                    {
                        Summary =
                            $"Returns a read-only span over the elements of this {GetTypeTriviaReference($"{EQUATABLE_ARRAY}{{T}}", "EquatableArray<T>")}.",
                        Returns = "A read-only span over the elements."
                    }
                },
                new MethodSource
                {
                    Name = "GetEnumerator",
                    Signature = "public partial global::System.Collections.Immutable.ImmutableArray<T>.Enumerator GetEnumerator()",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in context) =>
                    {
                        if (!HasConstructed(in context))
                        {
                            writer.AppendLine("return default;");
                            return;
                        }

                        writer.AppendLine("return AsImmutableArray().GetEnumerator();");
                    },
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
                    Implementation = (writer, in context) =>
                    {
                        if (!HasConstructed(in context))
                        {
                            writer.AppendLine("return default!;");
                            return;
                        }

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
                    Implementation = (writer, in context) =>
                    {
                        if (!HasConstructed(in context))
                        {
                            writer.AppendLine("return default!;");
                            return;
                        }

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
                    Implementation = (writer, in context) =>
                    {
                        if (!HasConstructed(in context))
                        {
                            writer.AppendLine("return default;");
                            return;
                        }

                        writer.AppendLine($"return {GLOBAL_MEMORY_EXT}.SequenceEqual<T>(AsSpan(), other.AsSpan());");
                    },
                    AlwaysWrite = true,
                    Trivia = new TriviaSource
                    {
                        Summary =
                            $"Determines whether this instance is equal to another {GetTypeTriviaReference($"{EQUATABLE_ARRAY}{{T}}", "EquatableArray<T>")}.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["other"] = "The other instance to compare."
                        },
                        Returns = "<see langword=\"true\"/> if this instance is equal to <paramref name=\"other\"/>; otherwise <see langword=\"false\"/>."
                    }
                },
                new MethodSource
                {
                    Name = "Equals",
                    Signature = "public override partial bool Equals(object? other)",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in context) =>
                    {
                        if (!HasConstructed(in context))
                        {
                            writer.AppendLine("return default;");
                            return;
                        }

                        writer.AppendLine($"return other is {GLOBAL_EQUATABLE_ARRAY}<T> array && Equals(this, array);");
                    },
                    AlwaysWrite = true,
                    Trivia = new TriviaSource
                    {
                        Summary = "Determines whether this instance is equal to the specified object.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["other"] = "The object to compare."
                        },
                        Returns = "<see langword=\"true\"/> if this instance is equal to <paramref name=\"other\"/>; otherwise <see langword=\"false\"/>."
                    }
                },
                new MethodSource
                {
                    Name = "GetHashCode",
                    Signature = "public override partial int GetHashCode()",
                    Attributes = AggressiveInlineAttribute,
                    Implementation = (writer, in context) =>
                    {
                        if (!HasConstructed(in context))
                        {
                            writer.AppendLine("return default;");
                            return;
                        }

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
                    Signature = "public static implicit operator global::" + EQUATABLE_ARRAY +
                                "<T>(global::System.Collections.Immutable.ImmutableArray<T> array)",
                    Implementation = (writer, in context) =>
                    {
                        if (!HasConstructed(in context))
                        {
                            writer.AppendLine("return default;");
                            return;
                        }

                        writer.AppendLine("return new global::" + EQUATABLE_ARRAY + "<T>(array);");
                    },
                    AlwaysWrite = true,
                    SkipPartial = true,
                    Trivia = new TriviaSource
                    {
                        Summary =
                            "Implicitly converts an <see cref=\"global::System.Collections.Immutable.ImmutableArray{T}\"/> to an <see cref=\"EquatableArray{T}\"/>.",
                        Returns = "The converted <see cref=\"EquatableArray{T}\"/>."
                    }
                },
                new MethodSource
                {
                    Name = "ImmutableArrayOperator",
                    Signature = "public static implicit operator global::System.Collections.Immutable.ImmutableArray<T>(global::" + EQUATABLE_ARRAY +
                                "<T> array)",
                    Implementation = (writer, in context) =>
                    {
                        if (!HasConstructed(in context))
                        {
                            writer.AppendLine("return default;");
                            return;
                        }

                        writer.AppendLine("return array.AsImmutableArray();");
                    },
                    AlwaysWrite = true,
                    SkipPartial = true,
                    Trivia = new TriviaSource
                    {
                        Summary =
                            "Implicitly converts an <see cref=\"EquatableArray{T}\"/> to an <see cref=\"global::System.Collections.Immutable.ImmutableArray{T}\"/>.",
                        Returns = "The converted <see cref=\"global::System.Collections.Immutable.ImmutableArray{T}\"/>."
                    }
                },
                new MethodSource
                {
                    Name = "==",
                    Signature = "public static bool operator ==(global::" + EQUATABLE_ARRAY + "<T> left, global::" + EQUATABLE_ARRAY + "<T> right)",
                    Implementation = (writer, in context) =>
                    {
                        if (!HasConstructed(in context))
                        {
                            writer.AppendLine("return default;");
                            return;
                        }

                        writer.AppendLine("return left.Equals(right);");
                    },
                    AlwaysWrite = true,
                    SkipPartial = true,
                    Trivia = new TriviaSource
                    {
                        Summary = "Determines whether two <see cref=\"EquatableArray{T}\"/> instances are equal.",
                        Returns = $"{TRIVIA_TRUE} if <paramref name=\"left\"/> is equal to <paramref name=\"right\"/>; otherwise {TRIVIA_FALSE}."
                    }
                },
                new MethodSource
                {
                    Name = "!=",
                    Signature = "public static bool operator !=(global::" + EQUATABLE_ARRAY + "<T> left, global::" + EQUATABLE_ARRAY + "<T> right)",
                    Implementation = (writer, in context) =>
                    {
                        if (!HasConstructed(in context))
                        {
                            writer.AppendLine("return default;");
                            return;
                        }

                        writer.AppendLine("return !left.Equals(right);");
                    },
                    AlwaysWrite = true,
                    SkipPartial = true,
                    Trivia = new TriviaSource
                    {
                        Summary = "Determines whether two <see cref=\"EquatableArray{T}\"/> instances are not equal.",
                        Returns = $"{TRIVIA_TRUE} if <paramref name=\"left\"/> is not equal to <paramref name=\"right\"/>; otherwise {TRIVIA_FALSE}."
                    }
                }
            ]
        };

        bool HasConstructed(in ImplementationContext context)
        {
            for (int i = 0; i < equatableArrayConstructors.Length; i++)
            {
                if (context.HasCalledMethod(equatableArrayConstructors[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}