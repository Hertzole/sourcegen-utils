using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    internal const string ARRAY_BUILDER = NAMESPACE + ".ArrayBuilder";

    private static TypeSource CreateArrayBuilder()
    {
        const string array_builder = ARRAY_BUILDER + "<T>";
        const string writer = array_builder + ".Writer";
        const string writer_no_generic = ARRAY_BUILDER + ".Writer";
        const string writer_pool = OBJECT_POOL + $"<global::{writer}>";

        string[] writerFieldDependencies =
        [
            writer_no_generic + ".Add",
            writer_no_generic + ".AddRange",
            writer_no_generic + ".Clear",
            writer_no_generic + ".RemoveAt"
        ];

        string[] constructorArgs =
        [
            ARRAY_BUILDER + ".Dispose()",
            writer_no_generic + ".Create()",
            OBJECT_POOL + ".ObjectPool(System.Func<T>, System.Action<T>, System.Action<T>, System.Action<T>)",
            OBJECT_POOL + ".Get()"
        ];

        return new TypeSource
        {
            Signature = "internal readonly partial struct ArrayBuilder<T> : global::System.IDisposable",
            Trivia = new TriviaSource
            {
                Summary = "A lightweight, pool-backed builder for constructing arrays. Dispose returns the backing buffer to the pool."
            },
            Fields = new Dictionary<string, FieldSource>
            {
                ["writer"] = new FieldSource
                {
                    Signature = $"private readonly global::{array_builder}.Writer writer = null!;",
                    Dependencies = [ARRAY_BUILDER + ".ArrayBuilder"]
                }
            },
            Methods =
            [
                new MethodSource
                {
                    Name = "ArrayBuilder",
                    Signature = "public partial ArrayBuilder()",
                    Implementation = (codeWriter, in _) => { codeWriter.AppendLine($"writer = {writer}.pool.Get();"); },
                    Dependencies = constructorArgs,
                    Trivia = new TriviaSource
                    {
                        Summary = "Creates a new <see cref=\"ArrayBuilder{T}\"/> with a default initial capacity."
                    }
                },
                new MethodSource
                {
                    Name = "ArrayBuilder",
                    Signature = "public partial ArrayBuilder(int capacity)",
                    Implementation = (codeWriter, in _) =>
                    {
                        codeWriter.AppendLine($"writer = {writer}.pool.Get();");
                        codeWriter.AppendLine("writer.EnsureCapacity(capacity);");
                    },
                    Dependencies = [.. constructorArgs, $"{writer_no_generic}.EnsureCapacity(int)"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Creates a new <see cref=\"ArrayBuilder{T}\"/> with the specified initial capacity.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["capacity"] = "The minimum capacity of the backing array."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "ArrayBuilder",
                    Signature = "public partial ArrayBuilder(global::System.ReadOnlySpan<T> items)",
                    Implementation = (codeWriter, in _) =>
                    {
                        codeWriter.AppendLine($"writer = {writer}.pool.Get();");
                        codeWriter.AppendLine("writer.AddRange(items);");
                    },
                    Dependencies = [.. constructorArgs, $"{writer_no_generic}.AddRange(System.ReadOnlySpan<T>)"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Creates a new <see cref=\"ArrayBuilder{T}\"/> initialized with the specified items.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["items"] = "The items to initialize the builder with."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "Add",
                    Signature = "public partial void Add(T item)",
                    Implementation = (codeWriter, in _) => { codeWriter.AppendLine("writer.Add(item);"); },
                    Dependencies = [$"{writer_no_generic}.Add(T)", $"{ARRAY_BUILDER}.Dispose()"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Adds an item to the builder.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["item"] = "The item to add."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "AddRange",
                    Signature = "public partial void AddRange(global::System.ReadOnlySpan<T> items)",
                    Implementation = (codeWriter, in _) => { codeWriter.AppendLine("writer.AddRange(items);"); },
                    Dependencies = [$"{writer_no_generic}.AddRange(System.ReadOnlySpan<T>)"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Adds a range of items to the builder.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["items"] = "The items to add."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "Remove",
                    Signature = "public partial bool Remove(T item)",
                    Implementation = (code, in _) =>
                    {
                        code.AppendLine("int index = global::System.Array.IndexOf(writer.array, item, 0, writer.index)");
                        code.AppendLine("if (index >= 0)");
                        using (code.WithBlock(true))
                        {
                            code.AppendLine("RemoveAt(index);");
                            code.AppendLine("return true;");
                        }

                        code.AppendLine("return false;");
                    },
                    EmptyStub = "return false;",
                    Dependencies = [ARRAY_BUILDER + ".RemoveAt(int)"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Removes the first occurrence of the specified item from the builder.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["item"] = "The item to remove."
                        },
                        Returns = "<c>true</c> if the item was found and removed; otherwise <c>false</c>."
                    }
                },
                new MethodSource
                {
                    Name = "RemoveAt",
                    Signature = "public partial void RemoveAt(int index)",
                    Implementation = (code, in _) => { code.AppendLine("writer.RemoveAt(index);"); },
                    Dependencies = [$"{writer_no_generic}.RemoveAt(int)"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Removes the item at the specified index.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["index"] = "The zero-based index of the item to remove."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "Clear",
                    Signature = "public partial void Clear()",
                    Implementation = (codeWriter, in _) => { codeWriter.AppendLine("writer.Clear();"); },
                    Dependencies = [$"{writer_no_generic}.Clear()"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Removes all items from the builder."
                    }
                },
                new MethodSource
                {
                    Name = "Dispose",
                    Signature = "public partial void Dispose()",
                    Implementation = (codeWriter, in _) => { codeWriter.AppendLine($"{writer}.pool.Return(writer);"); },
                    Dependencies = [writer_no_generic + $".OnReturn({writer})", OBJECT_POOL + ".Return(T)"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Returns the backing buffer to the pool."
                    }
                },
                new MethodSource
                {
                    Name = "AsSpan",
                    Signature = "public partial global::System.ReadOnlySpan<T> AsSpan()",
                    Implementation = (codeWriter, in _) =>
                    {
                        codeWriter.AppendLine("return global::System.MemoryExtensions.AsSpan(writer.array, 0, writer.size);");
                    },
                    Dependencies = [writer_no_generic + $".OnReturn({writer})", OBJECT_POOL + ".Return(T)"],
                    EmptyStub = "return default;",
                    Trivia = new TriviaSource
                    {
                        Summary = "Returns a read-only span over the builder's contents.",
                        Returns = "A read-only span over the builder's contents."
                    }
                },
                new MethodSource
                {
                    Name = "ToString",
                    Signature = "public override partial string ToString()",
                    Implementation = (codeWriter, in _) =>
                    {
                        codeWriter.AppendLine("return global::System.MemoryExtensions.AsSpan(writer.array, 0, writer.size).ToString();");
                    },
                    Dependencies = [writer_no_generic + $".OnReturn({writer})", OBJECT_POOL + ".Return(T)"],
                    EmptyStub = "return string.Empty;",
                    Trivia = new TriviaSource
                    {
                        Summary = "Returns the builder's contents as a string.",
                        Returns = "The builder's contents as a string."
                    }
                }
            ],
            Types = new Dictionary<string, TypeSource>
            {
                ["Writer"] = new TypeSource
                {
                    Signature = "private sealed partial class Writer",
                    Trivia = new TriviaSource
                    {
                        Summary = "Internal pool-backed buffer that manages the backing array for an <see cref=\"ArrayBuilder{T}\"/>."
                    },
                    Fields = new Dictionary<string, FieldSource>
                    {
                        ["pool"] = new FieldSource
                        {
                            Signature = $"internal static readonly global::{writer_pool} pool = new {writer_pool}(Create, onReturn: OnReturn);",
                            Dependencies = [ARRAY_BUILDER + ".ArrayBuilder", ARRAY_BUILDER + ".Dispose()"]
                        },
                        ["array"] = new FieldSource
                        {
                            Signature = "internal T[] array = global::System.Array.Empty<T>();",
                            Dependencies = writerFieldDependencies
                        },
                        ["size"] = new FieldSource
                        {
                            Signature = "internal int size;",
                            Dependencies = writerFieldDependencies
                        }
                    },
                    Methods =
                    [
                        new MethodSource
                        {
                            Name = "Add",
                            Signature = "internal partial void Add(T value)",
                            Implementation = (code, in _) =>
                            {
                                code.AppendLine("EnsureCapacity(size + 1);");
                                code.AppendLine("array[size++] = value;");
                            },
                            Dependencies = [$"{writer_no_generic}.EnsureCapacity(int)"]
                        },
                        new MethodSource
                        {
                            Name = "AddRange",
                            Signature = "internal partial void AddRange(global::System.ReadOnlySpan<T> items)",
                            Implementation = (code, in _) =>
                            {
                                code.AppendLine("EnsureCapacity(size + items.Length);");
                                code.AppendLine("items.CopyTo(global::System.MemoryExtensions.AsSpan(array, size));");
                                code.AppendLine("size += items.Length;");
                            },
                            Dependencies = [$"{writer_no_generic}.EnsureCapacity(int)"]
                        },
                        new MethodSource
                        {
                            Name = "RemoveAt",
                            Signature = "internal partial void RemoveAt(int index)",
                            Implementation = (code, in _) =>
                            {
                                code.AppendLine("size--;");
                                code.AppendLine("if (index < size)");
                                using (code.WithBlock(true))
                                {
                                    code.AppendLine("global::System.Array.Copy(array, index + 1, array, index, size - index);");
                                }

                                code.AppendLine("array[size] = default!;");
                            }
                        },
                        new MethodSource
                        {
                            Name = "Clear",
                            Signature = "internal partial void Clear()",
                            Implementation = (code, in _) =>
                            {
                                code.AppendLine("global::System.Array.Clear(array, 0, size);");
                                code.AppendLine("size = 0;");
                            }
                        },
                        new MethodSource
                        {
                            Name = "EnsureCapacity",
                            Signature = "internal void EnsureCapacity(int newCapacity)",
                            Implementation = (code, in _) =>
                            {
                                code.AppendLine("if (newCapacity > array.Length)");
                                using (code.WithBlock())
                                {
                                    code.AppendLine("if (array.Length == 0)");
                                    using (code.WithBlock(true))
                                    {
                                        code.AppendLine("array = global::System.Buffers.ArrayPool<T>.Shared.Rent(newCapacity);");
                                        code.AppendLine("return;");
                                    }

                                    code.AppendLine("T[] oldBuffer = array;");
                                    code.AppendLine("T[] newBuffer = global::System.Buffers.ArrayPool<T>.Shared.Rent(newCapacity);");
                                    code.AppendLine("global::System.Array.Copy(oldBuffer, newBuffer, oldBuffer.Length);");
                                    code.AppendLine("array = newBuffer;");
                                    code.AppendLine("global::System.Buffers.ArrayPool<T>.Shared.Return(oldBuffer, true);");
                                }
                            },
                            SkipPartial = true
                        },
                        new MethodSource
                        {
                            Name = "Create",
                            Signature = $"private static global::{writer} Create()",
                            Implementation = (code, in _) => { code.AppendLine($"return new global::{writer}();"); },
                            SkipPartial = true,
                            EmptyStub = "return null!;"
                        },
                        new MethodSource
                        {
                            Name = "OnReturn",
                            Signature = $"private static void OnReturn(global::{writer} item)",
                            Implementation = (code, in _) => { code.AppendLine("item.Clear();"); },
                            Dependencies = [$"{writer_no_generic}.Clear()"],
                            SkipPartial = true
                        }
                    ]
                }
            }
        };
    }

    private static TypeSource CreateArrayBuilderExtensions()
    {
        return new TypeSource
        {
            Signature = "internal static partial class ArrayBuilderExtensions",
            Trivia = new TriviaSource
            {
                Summary = "Extension methods for <see cref=\"Hertzole.SourceGen.ArrayBuilder{T}\"/>."
            },
            Methods =
            [
                new MethodSource
                {
                    Name = "AddRange",
                    Signature = $"public static partial void AddRange(this global::{NAMESPACE}.ArrayBuilder<char> builder, string? value)",
                    Implementation = (writer, in _) =>
                    {
                        writer.AppendLine("if (string.IsNullOrEmpty(value))");
                        using (writer.WithBlock(true))
                        {
                            writer.AppendLine("return;");
                        }

                        writer.AppendLine("builder.AddRange(global::System.MemoryExtensions.AsSpan(value));");
                    },
                    Dependencies = [$"{NAMESPACE}.ArrayBuilder.AddRange(System.ReadOnlySpan<T>)"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Appends a string to the builder. Does nothing if the value is <c>null</c> or empty.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["builder"] = "The character builder.",
                            ["value"] = "The string to append."
                        }
                    }
                }
            ]
        };
    }
}