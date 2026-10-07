using System;
using System.Runtime.CompilerServices;

namespace Hertzole.SourceGen;

internal partial struct ArrayBuilder<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveRange(int start, int count)
    {
        writer.RemoveRange(start, count);
    }

    public void Replace(T toReplace, T newValue)
    {
        int index = 0;
        do
        {
            index = Array.IndexOf(writer.array, toReplace, index, writer.size - index);
            if (index != -1)
            {
                writer.array[index] = newValue;
            }
        } while (index != -1);
    }

    private partial class Writer
    {
        internal void RemoveRange(int start, int count)
        {
            if (count > 0)
            {
                size -= count;
                if (start < size)
                {
                    Array.Copy(array, start + count, array, start, size - start);
                }

                if (typeof(T) != typeof(char))
                {
                    Array.Clear(array, size, count);
                }
            }
        }
    }
}