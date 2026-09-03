## 1.2.0 - Unreleased

### Added
- `EnableRecordSupportAttribute` - assembly level attribute for injecting support for records
- `EnableRequiredSupportAttribute` - assembly level attribute for injecting support for required members
- `ArrayBuilder<T>.ToImmutableArray()` for converting the builder contents to an immutable array
- `VariableNames.AppendGlobalPrefix` for appending `global::` to a span or string

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
