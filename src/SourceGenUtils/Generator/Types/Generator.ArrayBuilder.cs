using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private const string ARRAY_BUILDER_EXAMPLE =
        """
        The following example demonstrates how to add, remove, consume, and dispose the array builder.

        <code>
        using System;
        using System.Reflection;
        using Hertzole.SourceGen;

        public class MethodCaller
        {
            public void CallMethods(object target)
            {
                // Must dispose when the builder is no longer needed.
                using ArrayBuilder<MethodInfo> builder = new ArrayBuilder<MethodInfo>();

                CollectMethods(target, builder);
                CallCollectedMethods(target, builder.AsSpan());
            }

            private void CollectMethods(object target, ArrayBuilder<MethodInfo> builder)
            {
                Type type = target.GetType();
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                builder.AddRange(methods);
            }

            private void CallCollectedMethods(object target, ReadOnlySpan<MethodInfo> methods)
            {
                foreach (MethodInfo method in methods)
                {
                    // Methods like object.Equals(object) take parameters, so skip them.
                    if (method.GetParameters().Length == 0)
                    {
                        method.Invoke(target, null);
                    }
                }
            }
        }
        </code>

        The following example shows how you can use ArrayBuilder to quickly create strings.

        <code>
        using System;
        using Hertzole.SourceGen;

        public class Announcer
        {
            public void AnnounceName(int playerIndex)
            {
                // Must dispose when the builder is no longer needed.
                using ArrayBuilder<char> builder = new ArrayBuilder<char>();
                builder.AddRange("Player ");
                builder.AddRange(playerIndex.ToString());

                // ToString on ArrayBuilder<char> returns a constructed string.
                Console.WriteLine($"Welcome, {builder.ToString()}!");
            }
        }
        </code>
        """;

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
            writer_no_generic + ".RemoveAt",
            writer_no_generic + ".OnReturn"
        ];

        string[] constructorArgs =
        [
            ARRAY_BUILDER + ".Dispose()",
            writer_no_generic + ".Create()",
            OBJECT_POOL + ".ObjectPool(System.Func<T>, System.Action<T>, System.Action<T>, System.Action<T>)",
            OBJECT_POOL + ".Get()"
        ];

        TriviaSource insertRangeTrivia = new TriviaSource
        {
            Summary = "Inserts multiple items into the specified index and shifts the items after the newly inserted items to the right.",
            Parameters = new Dictionary<string, string>
            {
                ["index"] = "The zero-based index at which to insert the items.",
                ["items"] = "The items to insert."
            }
        };

        return new TypeSource
        {
            Signature = "internal readonly partial struct ArrayBuilder<T> : global::System.IDisposable",
            Trivia = new TriviaSource
            {
                Summary = "A lightweight, pool-backed builder for constructing arrays. Dispose returns the backing buffer to the pool.",
                Example = ARRAY_BUILDER_EXAMPLE
            },
            Fields = new Dictionary<string, FieldSource>
            {
                ["writer"] = new FieldSource
                {
                    Signature = $"private readonly global::{array_builder}.Writer writer = null!;",
                    Dependencies = [ARRAY_BUILDER + ".ArrayBuilder"]
                }
            },
            Properties = new Dictionary<string, PropertySource>
            {
                ["Count"] = new PropertySource
                {
                    Signature = "public int Count",
                    GetImplementation = (codeWriter, in context) =>
                    {
                        // If has constructed, it has a writer.
                        // Else just return 0.
                        codeWriter.AppendLine(context.HasCalledMethod($"{ARRAY_BUILDER}.ArrayBuilder") ? "return writer.size;" : "return 0;");
                    }
                },
                ["indexer"] = new PropertySource
                {
                    Signature = "public T this[int index]",
                    GetImplementation = (codeWriter, in context) =>
                    {
                        if (!context.HasCalledMethod($"{ARRAY_BUILDER}.ArrayBuilder"))
                        {
                            codeWriter.AppendLine("return default!;");
                            return;
                        }

                        codeWriter.AppendLine("if (index < 0 || index >= writer.size)");
                        using (codeWriter.WithBlock(true))
                        {
                            codeWriter.AppendLine("throw new global::System.ArgumentOutOfRangeException(nameof(index));");
                        }

                        codeWriter.AppendLine("return writer.array[index];");
                    },
                    SetImplementation = (codeWriter, in context) =>
                    {
                        if (!context.HasCalledMethod($"{ARRAY_BUILDER}.ArrayBuilder"))
                        {
                            return;
                        }

                        codeWriter.AppendLine("if (index < 0 || index >= writer.size)");
                        using (codeWriter.WithBlock(true))
                        {
                            codeWriter.AppendLine("throw new global::System.ArgumentOutOfRangeException(nameof(index));");
                        }

                        codeWriter.AppendLine("writer.array[index] = value;");
                    }
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
                        Summary = $"Creates a new {GetTypeTriviaReference($"{GLOBAL_ARRAY_BUILDER}{{T}}", "ArrayBuilder<T>")} with a default initial capacity."
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
                        Summary =
                            $"Creates a new {GetTypeTriviaReference($"{GLOBAL_ARRAY_BUILDER}{{T}}", "ArrayBuilder<T>")} with the specified initial capacity.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["capacity"] = "The minimum capacity of the backing array."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "ArrayBuilder",
                    Signature = $"public partial ArrayBuilder({GLOBAL_R_SPAN}<T> items)",
                    Implementation = (codeWriter, in _) =>
                    {
                        codeWriter.AppendLine($"writer = {writer}.pool.Get();");
                        codeWriter.AppendLine("writer.AddRange(items);");
                    },
                    Dependencies = [.. constructorArgs, $"{writer_no_generic}.AddRange({R_SPAN}<T>)"],
                    Trivia = new TriviaSource
                    {
                        Summary =
                            $"Creates a new {GetTypeTriviaReference($"{GLOBAL_ARRAY_BUILDER}{{T}}", "ArrayBuilder<T>")} initialized with the specified items.",
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
                    },
                    Attributes = AggressiveInlineAttribute
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
                    },
                    Attributes = AggressiveInlineAttribute
                },
                new MethodSource
                {
                    Name = "AddRange",
                    Signature = "public partial void AddRange(global::System.Collections.Generic.IEnumerable<T> items)",
                    Implementation = (codeWriter, in _) => { codeWriter.AppendLine("writer.AddRange(items);"); },
                    Dependencies = [$"{writer_no_generic}.AddRange(System.Collections.Generic.IEnumerable<T>)"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Adds a range of items to the builder.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["items"] = "The items to add."
                        }
                    },
                    Attributes = AggressiveInlineAttribute
                },
                new MethodSource
                {
                    Name = "Insert",
                    Signature = "public partial void Insert(int index, T item)",
                    Implementation = (codeWriter, in _) =>
                    {
                        codeWriter.AppendIndentedSource(
                            """
                            writer.EnsureCapacity(writer.size + 1);
                            global::System.Array.Copy(writer.array, index, writer.array, index + 1, writer.size - index);
                            writer.array[index] = item;
                            writer.size++;
                            """);
                    },
                    Trivia = new TriviaSource
                    {
                        Summary = "Inserts an item into the specified index and shifts the items after it to the right.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["index"] = "The zero-based index at which to insert the item.",
                            ["item"] = "The item to insert."
                        }
                    }
                },
                new MethodSource
                {
                    Name = "InsertRange",
                    Signature = "public partial void InsertRange(int index, global::System.ReadOnlySpan<T> items)",
                    Implementation = (codeWriter, in _) =>
                    {
                        codeWriter.AppendIndentedSource(
                            $$"""
                              int count = items.Length;

                              if (count == 0)
                              {
                                  return;
                              }

                              writer.EnsureCapacity(writer.size + count);
                              global::System.Array.Copy(writer.array, index, writer.array, index + count, writer.size - index);
                              items.CopyTo({{GLOBAL_MEMORY_EXT}}.AsSpan(writer.array, index, count));
                              writer.size += count;
                              """);
                    },
                    Trivia = insertRangeTrivia
                },
                new MethodSource
                {
                    Name = "InsertRange",
                    Signature = "public partial void InsertRange(int index, global::System.Collections.Generic.IEnumerable<T> items)",
                    Implementation = (codeWriter, in _) =>
                    {
                        codeWriter.AppendIndentedSource(
                            """
                            if (items is global::System.Collections.Generic.ICollection<T> c)
                            {
                                int count = c.Count;
                                if (count > 0)
                                {
                                    writer.EnsureCapacity(writer.size + count);
                                    global::System.Array.Copy(writer.array, index, writer.array, index + count, writer.size - index);
                                    c.CopyTo(writer.array, index);
                                    writer.size += count;
                                }
                            }
                            else
                            {
                                using (global::System.Collections.Generic.IEnumerator<T> en = items.GetEnumerator())
                                {
                                    while (en.MoveNext())
                                    {
                                        Insert(index++, en.Current);
                                    }
                                }
                            }
                            """);
                    },
                    Trivia = insertRangeTrivia,
                    Dependencies = [$"{ARRAY_BUILDER}.Insert(int, T)"]
                },
                new MethodSource
                {
                    Name = "Remove",
                    Signature = "public partial bool Remove(T item)",
                    Implementation = (code, in _) =>
                    {
                        code.AppendLine("int index = global::System.Array.IndexOf(writer.array, item, 0, writer.size);");
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
                        Returns = $"{TRIVIA_TRUE} if the item was found and removed; otherwise {TRIVIA_FALSE}."
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
                    },
                    Attributes = AggressiveInlineAttribute
                },
                new MethodSource
                {
                    Name = "RemoveRange",
                    Signature = "public partial void RemoveRange(int start, int count)",
                    Implementation = (codeWriter, in _) => { codeWriter.AppendLine("writer.RemoveRange(start, count);"); },
                    Dependencies = [$"{writer_no_generic}.RemoveRange(int, int)"],
                    Trivia = new TriviaSource
                    {
                        Summary = "Removes a range of items, starting at the specified start index and removes the specified count.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["start"] = "The zero-based index of the first item to remove.",
                            ["count"] = "The number of items to remove."
                        }
                    },
                    Attributes = AggressiveInlineAttribute
                },
                new MethodSource
                {
                    Name = "ReplaceAll",
                    Signature = "public partial void ReplaceAll(T toReplace, T newValue)",
                    Implementation = (codeWriter, in _) =>
                    {
                        codeWriter.AppendIndentedSource("""
                                                        int index = 0;
                                                        do
                                                        {
                                                            index = global::System.Array.IndexOf(writer.array, toReplace, index, writer.size - index);
                                                            if (index != -1)
                                                            {
                                                                writer.array[index] = newValue;
                                                            }
                                                        } while (index != -1);
                                                        """);
                    },
                    Trivia = new TriviaSource
                    {
                        Summary = "Replaces all values of the specified value in the array with the new specified value.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["toReplace"] = "The value to replace.",
                            ["newValue"] = "The new value to insert."
                        }
                    },
                    Attributes = AggressiveInlineAttribute
                },
                new MethodSource
                {
                    Name = "IndexOf",
                    Signature = "public partial int IndexOf(T item)",
                    Implementation = (code, in _) => { code.AppendLine("return global::System.Array.IndexOf(writer.array, item, 0, writer.size);"); },
                    EmptyStub = "return 0;",
                    Trivia = new TriviaSource
                    {
                        Summary = "Gets the index of an item in the array.",
                        Returns = "The index of the item in the array, or -1 if the item was not found.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["item"] = "The item to check for."
                        }
                    },
                    Attributes = AggressiveInlineAttribute
                },
                new MethodSource
                {
                    Name = "Contains",
                    Signature = "public partial bool Contains(T item)",
                    Implementation = (code, in _) => { code.AppendLine("return global::System.Array.IndexOf(writer.array, item, 0, writer.size) != -1;"); },
                    EmptyStub = "return false;",
                    Trivia = new TriviaSource
                    {
                        Summary = "Checks if the specified item is added to the array.",
                        Returns = $"{TRIVIA_TRUE} if the item was found; otherwise {TRIVIA_FALSE}.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["item"] = "The item to check for."
                        }
                    },
                    Attributes = AggressiveInlineAttribute
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
                    },
                    Attributes = AggressiveInlineAttribute
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
                    Signature = $"public partial {GLOBAL_R_SPAN}<T> AsSpan()",
                    Implementation = (codeWriter, in _) => { codeWriter.AppendLine($"return {GLOBAL_MEMORY_EXT}.AsSpan(writer.array, 0, writer.size);"); },
                    Dependencies = [writer_no_generic + $".OnReturn({writer})", OBJECT_POOL + ".Return(T)"],
                    EmptyStub = "return default;",
                    Trivia = new TriviaSource
                    {
                        Summary = "Returns a read-only span over the builder's contents.",
                        Returns = "A read-only span over the builder's contents."
                    },
                    Attributes = AggressiveInlineAttribute
                },
                new MethodSource
                {
                    Name = "ToArray",
                    Signature = "public partial T[] ToArray()",
                    Implementation = (codeWriter, in _) =>
                    {
                        codeWriter.AppendLine("T[] result =  new T[writer.size];");
                        codeWriter.AppendLine("global::System.Array.Copy(writer.array, result, writer.size);");
                        codeWriter.AppendLine("return result;");
                    },
                    Dependencies = [writer_no_generic + $".OnReturn({writer})", OBJECT_POOL + ".Return(T)"],
                    EmptyStub = "return default!;",
                    Trivia = new TriviaSource
                    {
                        Summary = "Returns a new array with the builder's contents.",
                        Returns = "A new array with the builder's contents."
                    }
                },
                new MethodSource
                {
                    Name = "ToImmutableArray",
                    Signature = "public partial global::System.Collections.Immutable.ImmutableArray<T> ToImmutableArray()",
                    Implementation = (codeWriter, in _) =>
                    {
                        codeWriter.AppendLine("return global::System.Collections.Immutable.ImmutableArray.Create(writer.array, 0, writer.size);");
                    },
                    EmptyStub = "return default;",
                    Trivia = new TriviaSource
                    {
                        Summary = "Returns a new immutable array with the builder's contents.",
                        Returns = "A new immutable array with the builder's contents."
                    },
                    Attributes = AggressiveInlineAttribute
                },
                new MethodSource
                {
                    Name = "CopyTo",
                    Signature = "public partial void CopyTo(T[] destination, int destinationIndex)",
                    Implementation = (codeWriter, in _) =>
                    {
                        codeWriter.AppendLine("global::System.Array.Copy(writer.array, 0, destination, destinationIndex, writer.size);");
                    },
                    Trivia = new TriviaSource
                    {
                        Summary = "Copies the contents of this array builder to the specified array.",
                        Parameters = new Dictionary<string, string>
                        {
                            ["destination"] = "The array to copy the contents to.",
                            ["destinationIndex"] = "The index into the destination array to which the first copied element is written."
                        }
                    },
                    Attributes = AggressiveInlineAttribute
                },
                new MethodSource
                {
                    Name = "ToString",
                    Signature = "public override partial string ToString()",
                    Implementation = (codeWriter, in _) =>
                    {
                        codeWriter.AppendLine("if (typeof(T) == typeof(char))");
                        using (codeWriter.WithBlock(true))
                        {
                            codeWriter.AppendLine($"return {GLOBAL_MEMORY_EXT}.AsSpan(writer.array, 0, writer.size).ToString();");
                        }

                        codeWriter.AppendLine($"return $\"{ARRAY_BUILDER}<{{typeof(T).Name}}>[{{writer.size}}]\";");
                    },
                    Dependencies = [writer_no_generic + $".OnReturn({writer})", OBJECT_POOL + ".Return(T)"],
                    EmptyStub = "return string.Empty;",
                    Trivia = new TriviaSource
                    {
                        Summary =
                            $"For {GetTypeTriviaReference($"{GLOBAL_ARRAY_BUILDER}{{Char}}", "ArrayBuilder<char>")}, returns a new string that represents the builder's contents.\n" +
                            $"Otherwise, returns a {GetTypeTriviaReference("global::System.String", "string")} with the name of the type and the number of elements."
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
                            Name = "AddRange",
                            Signature = "internal partial void AddRange(global::System.Collections.Generic.IEnumerable<T> items)",
                            Implementation = (code, in _) =>
                            {
                                code.AppendLine("if (items is global::System.Collections.Generic.ICollection<T> collection)");
                                using (code.WithBlock(true))
                                {
                                    code.AppendLine("EnsureCapacity(size + collection.Count);");
                                    code.AppendLine("collection.CopyTo(array, size);");
                                    code.AppendLine("size += collection.Count;");
                                    code.AppendLine("return;");
                                }

                                code.AppendLine("AddRange(global::System.MemoryExtensions.AsSpan(global::System.Linq.Enumerable.ToArray(items)));");
                            },
                            Dependencies = [$"{writer_no_generic}.EnsureCapacity(int)", $"{writer_no_generic}.AddRange({R_SPAN}<T>)"]
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
                            Name = "RemoveRange",
                            Signature = "internal partial void RemoveRange(int start, int count)",
                            Implementation = (code, in _) =>
                            {
                                code.AppendIndentedSource("""
                                                          if (count > 0)
                                                          {
                                                              size -= count;
                                                              if (start < size)
                                                              {
                                                                  global::System.Array.Copy(array, start + count, array, start, size - start);
                                                              }

                                                              if (typeof(T) != typeof(char))
                                                              {
                                                                  global::System.Array.Clear(array, size, count);
                                                              }
                                                          }
                                                          """);
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
                            Implementation = (code, in _) =>
                            {
                                code.AppendLine("if (item.array.Length > 0)");
                                using (code.WithBlock(true))
                                {
                                    code.AppendLine("global::System.Buffers.ArrayPool<T>.Shared.Return(item.array, typeof(T) != typeof(char));");
                                    code.AppendLine("item.array = global::System.Array.Empty<T>();");
                                }

                                code.AppendLine("item.size = 0;");
                            },
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
                Summary = $"Extension methods for {GetTypeTriviaReference($"{GLOBAL_ARRAY_BUILDER}{{T}}", "ArrayBuilder<T>")}."
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
                        Summary = $"Appends a string to the builder. Does nothing if the value is {TRIVIA_NULL} or empty.",
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