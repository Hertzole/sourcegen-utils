using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Hertzole.SourceGen;

namespace SourceGenUtils.WrapperGenerator;

[Flags]
public enum MethodFlags
{
    None = 0,
    Static = 1 << 0,
    Operator = 1 << 1,
    Override = 1 << 2,
    Implicit = 1 << 3,
    Explicit = 1 << 4,
    Constructor = 1 << 5,
    ExplicitImplementation = 1 << 6,
    HasRefType = 1 << 7,
    Finalizer = 1 << 8,
    HasOutParameter = 1 << 9,
    HasRefParameter = 1 << 10
}

internal readonly ref struct SourceMethodSignature
{
    private readonly MethodFlags flags;
    private readonly BindingFlags bindingFlags;

    public readonly ReadOnlySpan<char> Name;
    public readonly ReadOnlySpan<char> ReturnType;
    public readonly ReadOnlySpan<char> Accessor;

    private static readonly Regex explicitImplementationRegex =
        new Regex(@"(.*) (.*\..*)\(.*?\)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    public bool IsStatic
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return (flags & MethodFlags.Static) != 0; }
    }

    public bool IsOperator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return (flags & MethodFlags.Operator) != 0; }
    }

    public bool IsOverride
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return (flags & MethodFlags.Override) != 0; }
    }

    public bool IsImplicit
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return (flags & MethodFlags.Implicit) != 0; }
    }

    public bool IsExplicit
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return (flags & MethodFlags.Explicit) != 0; }
    }

    public bool IsConstructor
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return (flags & MethodFlags.Constructor) != 0; }
    }

    public bool IsExplicitImplementation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return (flags & MethodFlags.ExplicitImplementation) != 0; }
    }

    public bool HasRefStruct
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return (flags & MethodFlags.HasRefType) != 0; }
    }

    public bool IsVoidReturnType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return IsConstructor || ReturnType.Equals("void", StringComparison.Ordinal); }
    }

    public bool HasOutParameter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return (flags & MethodFlags.HasOutParameter) != 0; }
    }

    public bool HasRefParameter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return (flags & MethodFlags.HasRefParameter) != 0; }
    }

    private SourceMethodSignature(ReadOnlySpan<char> name,
        ReadOnlySpan<char> returnType,
        ReadOnlySpan<char> accessor,
        MethodFlags flags,
        BindingFlags bindingFlags)
    {
        this.flags = flags;
        this.bindingFlags = bindingFlags;
        Name = name;
        ReturnType = returnType;
        Accessor = accessor;
    }

    public static SourceMethodSignature FromSignature(ReadOnlySpan<char> signature)
    {
        try
        {
            ReadOnlySpan<char> accessorSlice = ReadOnlySpan<char>.Empty;
            ReadOnlySpan<char> nameSlice;
            ReadOnlySpan<char> returnTypeSlice = ReadOnlySpan<char>.Empty;

            BindingFlags bindingFlags = BindingFlags.Default;

            if (signature.StartsWith("public", StringComparison.Ordinal))
            {
                accessorSlice = signature.Slice(0, 6);
                bindingFlags |= BindingFlags.Public;
            }
            else if (signature.StartsWith("private", StringComparison.Ordinal))
            {
                accessorSlice = signature.Slice(0, 7);
                bindingFlags |= BindingFlags.NonPublic;
            }
            else if (signature.StartsWith("internal", StringComparison.Ordinal))
            {
                accessorSlice = signature.Slice(0, 8);
                bindingFlags |= BindingFlags.NonPublic;
            }
            else if (signature.StartsWith("protected", StringComparison.Ordinal))
            {
                accessorSlice = signature.Slice(0, 10);
                bindingFlags |= BindingFlags.NonPublic;
            }
            else if (TryGetExplicitImplementation(signature, out returnTypeSlice, out nameSlice))
            {
                bindingFlags |= BindingFlags.Instance;
                return new SourceMethodSignature(nameSlice, returnTypeSlice, accessorSlice, MethodFlags.ExplicitImplementation, bindingFlags);
            }
            else if (signature[0] == '~')
            {
                bindingFlags |= BindingFlags.Instance;
                return new SourceMethodSignature(signature.Slice(1), null, null, MethodFlags.Finalizer, bindingFlags);
            }

            int startParamsIndex = signature.IndexOf('(');
            int lastSpaceBeforeParameters = signature.Slice(0, startParamsIndex).LastIndexOf(' ');
            int operatorIndex = signature.IndexOf("operator", StringComparison.Ordinal);

            bool isConstructor = false;
            bool isStatic = signature.Contains("static", StringComparison.Ordinal);
            bool isOperator = operatorIndex != -1;
            bool isOverride = signature.Contains("override", StringComparison.Ordinal);
            bool isImplicit = signature.Contains("implicit", StringComparison.Ordinal);
            bool isExplicit = signature.Contains("explicit", StringComparison.Ordinal);
            bool hasRefType = signature.Contains("ReadOnlySpan<", StringComparison.Ordinal) || signature.Contains("Span<", StringComparison.Ordinal);
            bool hasOutParam = signature.Contains("out ", StringComparison.Ordinal);
            bool hasRefParam = signature.Contains("ref ", StringComparison.Ordinal);

            if (isStatic)
            {
                bindingFlags |= BindingFlags.Static;
            }
            else
            {
                bindingFlags |= BindingFlags.Instance;
            }

            if (isOperator)
            {
                nameSlice = signature.Slice(operatorIndex + 8, startParamsIndex - operatorIndex - 8).Trim();
                if (!isImplicit && !isExplicit)
                {
                    int untilName = signature.Slice(0, operatorIndex).LastIndexOf(' ');
                    ReadOnlySpan<char> untilNameSlice = signature.Slice(0, untilName);
                    int lastSpaceBeforeType = untilNameSlice.LastIndexOf(' ');
                    if (lastSpaceBeforeType != -1)
                    {
                        untilNameSlice = untilNameSlice.Slice(lastSpaceBeforeType + 1);
                    }

                    returnTypeSlice = untilNameSlice.Trim();
                }
            }
            else
            {
                nameSlice = signature.Slice(lastSpaceBeforeParameters + 1, startParamsIndex - lastSpaceBeforeParameters - 1).Trim();
            }

            if (!isImplicit && !isExplicit && !isOperator)
            {
                // implicit/explicit doesn't have return type

                ReadOnlySpan<char> untilName = signature.Slice(0, lastSpaceBeforeParameters).Trim();
                int spaceBeforeType = untilName.LastIndexOf(' ');
                if (spaceBeforeType != -1)
                {
                    returnTypeSlice = untilName.Slice(spaceBeforeType + 1).Trim();

                    if (returnTypeSlice.Equals("partial", StringComparison.Ordinal))
                    {
                        returnTypeSlice = ReadOnlySpan<char>.Empty;
                    }
                }
            }

            if ((isImplicit || isExplicit) && isOperator)
            {
                returnTypeSlice = nameSlice;
            }

            if (!isImplicit && !isExplicit && !isOperator && returnTypeSlice.IsEmpty)
            {
                isConstructor = true;
            }

            MethodFlags flags = MethodFlags.None;

            if (isConstructor)
            {
                flags |= MethodFlags.Constructor;
            }

            if (isStatic)
            {
                flags |= MethodFlags.Static;
            }

            if (isOverride)
            {
                flags |= MethodFlags.Override;
            }

            if (isOperator)
            {
                flags |= MethodFlags.Operator;
            }

            if (isImplicit)
            {
                flags |= MethodFlags.Implicit;
            }

            if (isExplicit)
            {
                flags |= MethodFlags.Explicit;
            }

            if (hasRefType)
            {
                flags |= MethodFlags.HasRefType;
            }

            if (hasOutParam)
            {
                flags |= MethodFlags.HasOutParameter;
            }

            if (hasRefParam)
            {
                flags |= MethodFlags.HasRefParameter;
            }

            return new SourceMethodSignature(nameSlice, returnTypeSlice, accessorSlice, flags, bindingFlags);
        }

        catch (Exception)
        {
            Log.Error($"Couldn't parse signature '{signature.ToString()}'");

            throw;
        }
    }

    public void AppendFlags(CodeWriter writer)
    {
        writer.AppendBindingFlags(bindingFlags);
    }

    private static bool TryGetExplicitImplementation(ReadOnlySpan<char> signature, out ReadOnlySpan<char> returnType, out ReadOnlySpan<char> name)
    {
        Match match = explicitImplementationRegex.Match(signature.ToString());

        if (match.Success && match.Groups.Count == 3)
        {
            returnType = match.Groups[1].Value.AsSpan();
            name = match.Groups[2].Value.AsSpan();
            return true;
        }

        returnType = ReadOnlySpan<char>.Empty;
        name = ReadOnlySpan<char>.Empty;
        return false;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return
            $"{nameof(Name)}: '{Name.ToString()}', {nameof(ReturnType)}: '{ReturnType.ToString()}', {nameof(Accessor)}: '{Accessor.ToString()}', " +
            $"{nameof(IsStatic)}: {IsStatic}, {nameof(IsOperator)}: {IsOperator}, {nameof(IsOverride)}: {IsOverride}, {nameof(IsImplicit)}: {IsImplicit}, " +
            $"{nameof(IsExplicit)}: {IsExplicit}, {nameof(IsConstructor)}: {IsConstructor}";
    }
}