using System;

namespace Hertzole.SourceGen;

internal partial struct ArrayBuilder<T>
{
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
}