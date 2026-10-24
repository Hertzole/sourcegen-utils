## 1.3.0 - Unreleased

### Added

- Assembly trimmer to automatically remove all unused types and members from SourceGen Utils
- `ArrayBuilder<T>.RemoveRange(int start, int count)` to remove a range of elements from the builder
- `ArrayBuilder<T>.ReplaceAll(T toReplace, T newValue)` to replace all occurrences of a value in the builder

### Fixed

- `EquatableArray` equals going recursive
- Compilation errors when using `SyntaxExtensions.GetAttributeSymbol`
- First indent not being written in `CodeWriter` until `AppendLine` has been called
- `SymbolExtensions.GetDeclarationString` not supporting interfaces / `CodeWriter.Append(ITypeSymbol)` not supporting
  interfaces
- `ObjectPool<T>` (and the collection pools) is now thread-safe

## 1.2.0 - 2026-09-05

### Added

- `EnableRecordSupportAttribute` - assembly level attribute for injecting support for records
- `EnableRequiredSupportAttribute` - assembly level attribute for injecting support for required members
- `ArrayBuilder<T>.ToImmutableArray()` for converting the builder contents to an immutable array
- `VariableNames.AppendGlobalPrefix` for appending `global::` to a span or string
- `SyntaxExtensions.HasAttribute(string)` for checking if a symbol has a specific attribute
- `SyntaxExtensions.TryGetAttribute(string, out AttributeData)` for checking if a symbol has a specific attribute and retrieving the attribute data
- `StringBuilderExtensions.Append(ReadOnlySpan<char>)` for appending a read-only character span to a StringBuilder instance
- `StringBuilderExtensions.AppendLine(ReadOnlySpan<char>)` for appending a read-only character span with a new-line
- `CodeWriter.AppendIndentedSource(string)` for appending an entire block of code with the proper indentation

### Fixed

- `CodeWriter.AppendLine(ITypeSymbol, bool, bool)` not generating `CodeWriter.Append(ITypeSymbol, bool, bool)`

## 1.1.0 - 2026-08-01

### Added

- `CodeWriter.WithBlock(bool newLineOnDispose = false)` for automatic newline when block is disposed
- `ArrayBuilder<T>.AddRange(IEnumerable<T>)` for adding generic `IEnumerable` collections
- `ArrayBuilder<T>.ToArray()` for converting the builder to an array
- `ArrayBuilder<T>.IndexOf(T)` for finding the index of an element in the builder
- `ArrayBuilder<T>.Contains(T)` for determining if an element exists in the builder
- `ArrayBuilder<T>.Count` for getting the number of elements in the builder
- `ArrayBuilder<T>` indexer for getting and setting elements by index

### Fixed

- Lots of XML documentation fixes, both for wrong and missing documentation
- `ArrayBuilder<T>.ToString()` not returning a proper string representation when not using `char` as generic argument
- Compilation error in `ArrayBuilder<T>.Remove`

## 1.0.0 - 2026-08-01

Initial release
