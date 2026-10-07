using System;
using Hertzole.SourceGen;

namespace SourceGenUtils.WrapperGenerator;

internal readonly record struct SourceParameterInfo(string Type, string Name, bool IsOut, bool IsRef, string? DefaultValue)
{
    public bool IsReferenceValue
    {
        get { return IsRef || IsOut; }
    }

    public static void FromSignature(ReadOnlySpan<char> signature, in ArrayBuilder<SourceParameterInfo> builder)
    {
        signature = signature.Slice(signature.IndexOf('(') + 1);
        int fail = 0;
        while (true)
        {
            bool isOut = false;
            bool isRef = false;
            if (signature.StartsWith("this "))
            {
                signature = signature.Slice(5);
            }
            else if (signature.StartsWith("out ") || signature.StartsWith("ref "))
            {
                isOut = signature.StartsWith("out");
                isRef = signature.StartsWith("ref");
                signature = signature.Slice(4);
            }

            int end = signature.IndexOf(',');
            if (end == -1)
            {
                end = signature.IndexOf(')');
                if (end == -1)
                {
                    return;
                }
            }

            int endOfType = signature.IndexOf(' ');
            if (endOfType == -1)
            {
                return;
            }

            ReadOnlySpan<char> typeSlice = signature.Slice(0, endOfType).Trim();
            ReadOnlySpan<char> afterType = signature.Slice(endOfType).Trim();

            int endOfName = afterType.IndexOf(',');
            if (endOfName == -1)
            {
                endOfName = afterType.IndexOf(')');
            }

            ReadOnlySpan<char> nameSlice = afterType.Slice(0, endOfName).Trim();
            int defaultIndex = nameSlice.IndexOf(" = ");
            ReadOnlySpan<char> defaultSlice = ReadOnlySpan<char>.Empty;
            if (defaultIndex != -1)
            {
                defaultSlice = nameSlice.Slice(defaultIndex + 3).Trim();
                nameSlice = nameSlice.Slice(0, defaultIndex);
            }

            builder.Add(new SourceParameterInfo(typeSlice.ToString(), nameSlice.ToString(), isOut, isRef,
                defaultSlice.IsEmpty ? string.Empty : defaultSlice.ToString()));

            signature = signature.Slice(end + 1).Trim();

            fail++;

            if (fail == 1000)
            {
                Log.Error($"Failed to append parameters. Signature: {signature.ToString()}");
                break;
            }
        }
    }
}