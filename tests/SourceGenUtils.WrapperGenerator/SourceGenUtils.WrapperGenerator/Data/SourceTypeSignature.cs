using System;

namespace SourceGenUtils.WrapperGenerator;

internal readonly ref struct SourceTypeSignature
{
    public readonly ReadOnlySpan<char> Name;
    public readonly ReadOnlySpan<char> GenericParameters;
    public readonly bool IsStruct;
    public readonly bool IsReadOnly;
    public readonly bool IsGeneric;
    public readonly int GenericArgsCount;

    private SourceTypeSignature(ReadOnlySpan<char> name,
        ReadOnlySpan<char> genericParameters,
        bool isStruct,
        bool isReadOnly,
        bool isGeneric,
        int genericArgsCount)
    {
        Name = name;
        GenericParameters = genericParameters;
        IsStruct = isStruct;
        IsReadOnly = isReadOnly;
        IsGeneric = isGeneric;
        GenericArgsCount = genericArgsCount;
    }

    public static SourceTypeSignature FromSignature(ReadOnlySpan<char> signature)
    {
        bool isStruct = signature.Contains("struct", StringComparison.Ordinal);
        bool isReadOnly = isStruct && signature.Contains("readonly", StringComparison.Ordinal);

        int genericArgsStart = signature.IndexOf('<');
        bool isGeneric = genericArgsStart != -1;

        ReadOnlySpan<char> genericParameters = ReadOnlySpan<char>.Empty;
        int genericCount = 0;

        if (isGeneric)
        {
            // The type parameter list is between the first '<' and its matching '>'.
            int depth = 0;
            int genericArgsEnd = -1;

            for (int i = genericArgsStart; i < signature.Length; i++)
            {
                if (signature[i] == '<')
                {
                    depth++;
                }
                else if (signature[i] == '>')
                {
                    depth--;
                    if (depth == 0)
                    {
                        genericArgsEnd = i;
                        break;
                    }
                }
            }

            genericParameters = signature.Slice(genericArgsStart + 1, genericArgsEnd - genericArgsStart - 1);

            genericCount = 1;
            foreach (char c in genericParameters)
            {
                if (c == ',')
                {
                    genericCount++;
                }
            }
        }

        return new SourceTypeSignature(signature, genericParameters, isStruct, isReadOnly, isGeneric, genericCount);
    }
}