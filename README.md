# SourceGen Utils

C# incremental source generator that emits common utility types directly into your assembly. Zero runtime dependencies,
zero unused code - only what you call gets generated.

## Features

- **Only include what you use** - methods you never call are never generated
- **Zero runtime dependencies** - everything is emitted as source into your project, no external library references
  needed
- **Code writer** - a `StringBuilder`-based code writer with automatic indentation and a fluent API
- **Performant** - heavy use of spans and pools in generated code

## Installation

### NuGet

```
dotnet add package Hertzole.SourceGenUtils
```

### Package Reference

```xml

<PackageReference Include="Hertzole.SourceGenUtils" Version="*"/>
```

## Generated Utilities

All types are emitted under the `Hertzole.SourceGen` namespace.

### CodeWriter

A `StringBuilder`-based code writer with automatic indentation and a fluent API.

```csharp
// Must be disposed to return string buffers
using var writer = new CodeWriter();
writer
    // Wraps your code in a namespace with automatic indentation
    .AppendNamespace("MyApp.Models")
    .AppendLine("public class User");

// Wraps your code in a block ({ }) with automatic indentation
using (writer.WithBlock())
{
    writer.AppendLine("public string Name { get; set; }");
    writer.AppendLine("public string GetName()");
    using (writer.WithBlock())
    {
        writer.AppendLine("if (string.IsNullOrEmpty(Name))");
        // Indents 4 spaces
        writer.Indent++;
        writer.AppendLine("return \"No name\"");
        // Indents back
        writer.Indent--;
        writer.AppendLine("return Name");
    }
}

// Use ToString() to get the final result
Console.WriteLine(writer.ToString());
```

Supports all the built-in types, appending the current line, and creating new lines. Also has helper methods for
appending common operations, like namespaces, `[GeneratedCode]` attribute, preprocessor symbols, and more.

### ObjectPool\<T\>

Stack-based object pool with factory and lifecycle callbacks (`onGet`, `onReturn`, `onDispose`).

```csharp
// Pool should be disposed when no longer used
using var pool = new ObjectPool<StringBuilder>(
    create: () => new StringBuilder(),
    onGet: sb => sb.Clear(),
    onReturn: sb => { }
);

// Gets from the pool
var sb = pool.Get();

// Return to pool
pool.Return(sb);

// Use a scope to automatically return items
using (var scope = pool.Get(out var sb))
{
    sb.Append("hello");
}
```

### Collection Pools

Static pools for common collections. All follow the same `Get()` / `Get(out T)` / `Return()` pattern with automatic
clearing on return.

| Type                | Wraps                                   |
|---------------------|-----------------------------------------|
| `StringBuilderPool` | `StringBuilder` (initial capacity 1024) |
| `ListPool<T>`       | `List<T>`                               |
| `HashSetPool<T>`    | `HashSet<T>`                            |
| `StackPool<T>`      | `Stack<T>`                              |
| `QueuePool<T>`      | `Queue<T>`                              |

```csharp
using var list = ListPool<int>.Get(out var items);
items.Add(42);
// returned and cleared on dispose
```

### EquatableArray\<T\>

`readonly struct` providing value-based equality for arrays.

```csharp
var a = new EquatableArray<string>(new[] { "hello", "world" });
var b = new EquatableArray<string>(new[] { "hello", "world" });
Console.WriteLine(a == b); // True
```

### ArrayBuilder\<T\>

Pool-backed array builder using `ArrayPool<T>.Shared`. Supports `Add`, `AddRange`, `Remove`, `RemoveAt`, and `Clear`.
More performant than `List<T>` when you just need to quickly construct arrays.

**Bonus!** Calling `ToString()` on `ArrayBuilder<char>` returns the built string, like a `StringBuilder`.

```csharp
using var builder = new ArrayBuilder<int>();
builder.Add(1);
builder.Add(2);
builder.AddRange(stackalloc[] { 3, 4 });
// buffer returned to pool on dispose
```

### VariableNames

Variable name transformation utilities for code generation. Nicify, remove prefixes (`m_`, `_`, `k`), uppercase start,
and detect event handler patterns. All, of course, support spans along with simply returning strings.

```csharp
VariableNames.NicifyVariableName("m_playerName"); // "PlayerName"
VariableNames.RemovePrefix("kConstant");          // "Constant"
VariableNames.UppercaseStart("playerPoints");     // "PlayerPoints"
```

### SyntaxExtensions

Roslyn syntax helpers to resolve attribute symbols and find field declarations from nested syntax nodes.

### Log

Debug-only file-based logging (`#if DEBUG`). Writes timestamped entries to `{AssemblyName}.log`.

You don't need to put them in a `#if DEBUG` preprocessor symbol, they simply just get removed in release builds!

```csharp
Log.Info("Generation started");
Log.Warning("Unsupported syntax node");
Log.Error("Failed to resolve symbol");
```

## How It Works

The generator operates in two phases:

1. **Shell generation** - emits `partial` type stubs so your code can reference the utilities during compilation.
2. **Incremental implementation** - scans for invocations and object creation expressions matching known types. Only
   called methods get full implementations. Uncalled methods compile as empty stubs.

This means adding the NuGet package has a minimal impact on your assembly size until you actually use a utility. When
nothing is used, it adds roughly 10 kb to your assembly.

## Requirements

- .NET Standard 2.0+ (source generator target)
- Roslyn 4.x+

## License

[MIT](LICENSE)
