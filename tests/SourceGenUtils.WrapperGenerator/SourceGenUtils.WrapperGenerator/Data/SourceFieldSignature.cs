using System;
using System.Reflection;

namespace SourceGenUtils.WrapperGenerator;

internal readonly ref struct SourceFieldSignature
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

    private SourceFieldSignature(BindingFlags flags, ReadOnlySpan<char> name, ReadOnlySpan<char> type)
    {
        this.flags = flags;
        Name = name;
        Type = type;
    }

    public static SourceFieldSignature FromSignature(ReadOnlySpan<char> signature)
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

        int endOfName = signature.IndexOf('=');
        if (endOfName == -1)
        {
            endOfName = signature.IndexOf(';');
            if (endOfName == -1)
            {
                throw new ArgumentException("Invalid signature");
            }
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

        return new SourceFieldSignature(flags, nameSlice, toTypeSlice);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{nameof(Name)}: {Name.ToString()}, {nameof(Type)}: {Type.ToString()}, Flags: {flags}";
    }
}