using System;
using System.Collections.Generic;

namespace Hertzole.SourceGenUtils;

internal sealed class OnlyMethodNameEquality : IEqualityComparer<string>
{
    public static OnlyMethodNameEquality Instance { get; } = new OnlyMethodNameEquality();

    /// <inheritdoc />
    public bool Equals(string x, string y)
    {
        ReadOnlySpan<char> xSpan = x.AsSpan();
        ReadOnlySpan<char> ySpan = y.AsSpan();

        int xPos = xSpan.IndexOf('(');
        int yPos = ySpan.IndexOf('(');

        if (xPos == -1 && yPos == -1)
        {
            return string.Equals(x, y, StringComparison.Ordinal);
        }

        if (xPos >= 0)
        {
            xSpan = xSpan.Slice(0, xPos);
        }

        if (yPos >= 0)
        {
            ySpan = ySpan.Slice(0, yPos);
        }

        return xSpan.SequenceEqual(ySpan);
    }

    /// <inheritdoc />
    public int GetHashCode(string obj)
    {
        ReadOnlySpan<char> span = obj.AsSpan();
        int pos = span.IndexOf('(');

        if (pos >= 0)
        {
            span = span.Slice(0, pos);
        }

        unchecked
        {
            int hash = span.Length;

            for (int i = 0; i < span.Length; i++)
            {
                hash = hash * 397 ^ span[i];
            }

            return hash;
        }
    }
}