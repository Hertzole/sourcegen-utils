using System.Reflection;
using Hertzole.SourceGen;

namespace SourceGenUtils.WrapperGenerator;

internal static class BindingFlagsExtensions
{
    public static void AppendBindingFlags(this CodeWriter writer, BindingFlags flags)
    {
        int i = 0;

        const string prefix = "global::System.Reflection.BindingFlags";

        if ((flags & BindingFlags.Public) != 0)
        {
            writer.Append($"{prefix}.Public");
            i++;
        }
        else if ((flags & BindingFlags.NonPublic) != 0)
        {
            writer.Append($"{prefix}.NonPublic");
            i++;
        }

        if (i > 0)
        {
            writer.Append(" | ");
        }

        if ((flags & BindingFlags.Static) != 0)
        {
            writer.Append($"{prefix}.Static");
        }
        else if ((flags & BindingFlags.Instance) != 0)
        {
            writer.Append($"{prefix}.Instance");
        }
    }
}