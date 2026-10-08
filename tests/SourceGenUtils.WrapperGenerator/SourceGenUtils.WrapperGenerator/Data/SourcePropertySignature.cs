using System;
using System.Reflection;
using Hertzole.SourceGen;

namespace SourceGenUtils.WrapperGenerator;

internal readonly ref struct SourcePropertySignature
{
    private readonly BindingFlags flags;

    public readonly ReadOnlySpan<char> Name;
    public readonly ReadOnlySpan<char> Type;

    public bool IsPublic
    {
        get { return (flags & BindingFlags.Public) != 0; }
    }

    public bool IsStatic
    {
        get { return (flags & BindingFlags.Static) != 0; }
    }

    public bool IsIndexer { get; }

    public bool HasImplicitGetter { get; }
    public bool HasImplicitSetter { get; }

    private SourcePropertySignature(BindingFlags flags,
        ReadOnlySpan<char> name,
        ReadOnlySpan<char> type,
        bool isIndexer,
        bool hasImplicitGetter = false,
        bool hasImplicitSetter = false)
    {
        this.flags = flags;
        Name = name;
        Type = type;
        IsIndexer = isIndexer;
        HasImplicitGetter = hasImplicitGetter;
        HasImplicitSetter = hasImplicitSetter;
    }

    public static SourcePropertySignature FromSignature(ReadOnlySpan<char> signature)
    {
        BindingFlags flags = BindingFlags.Default;

        if (signature.StartsWith("public", StringComparison.Ordinal))
        {
            flags |= BindingFlags.Public;
        }
        else
        {
            flags |= BindingFlags.NonPublic;
        }

        if (signature.Contains("static", StringComparison.Ordinal))
        {
            flags |= BindingFlags.Static;
        }
        else
        {
            flags |= BindingFlags.Instance;
        }

        bool hasImplicitGetter = signature.Contains("get;", StringComparison.Ordinal);
        bool hasImplicitSetter = signature.Contains("set;", StringComparison.Ordinal);

        int endOfName = signature.Length;

        int indexerStart = signature.IndexOf('[');
        bool isIndexer = indexerStart != -1;
        if (isIndexer)
        {
            endOfName = indexerStart;
        }

        ReadOnlySpan<char> toNameSlice = signature.Slice(0, endOfName).Trim();
        int endOfType = toNameSlice.LastIndexOf(' '); // the space between type and name (... bool field;)
        if (endOfType == -1)
        {
            throw new ArgumentException("Invalid signature");
        }

        ReadOnlySpan<char> toTypeSlice = signature.Slice(0, endOfType).Trim();
        int beforeTypeIndex = toTypeSlice.LastIndexOf(' ');
        if (beforeTypeIndex != -1)
        {
            toTypeSlice = toTypeSlice.Slice(beforeTypeIndex + 1);
        }

        ReadOnlySpan<char> nameSlice = signature.Slice(endOfType, endOfName - endOfType).Trim();

        return new SourcePropertySignature(flags, nameSlice, toTypeSlice, isIndexer, hasImplicitGetter, hasImplicitSetter);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return
            $"{nameof(Name)}: {Name.ToString()}, {nameof(Type)}: {Type.ToString()}, {nameof(IsIndexer)}: {IsIndexer}, Flags: {flags}, {nameof(HasImplicitGetter)}: {HasImplicitGetter}, {nameof(HasImplicitSetter)}: {HasImplicitSetter}";
    }

    public void AppendFlags(CodeWriter writer)
    {
        writer.AppendBindingFlags(flags);
    }
}