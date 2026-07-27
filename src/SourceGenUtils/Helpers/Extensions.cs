using System.Text;

namespace Hertzole.SourceGenUtils;

internal static class Extensions
{
    public static StringBuilder AppendLineUnix(this StringBuilder sb, string? value)
    {
        sb.Append(value);
        sb.Append('\n');
        return sb;
    }

    public static StringBuilder AppendLineUnix(this StringBuilder sb)
    {
        sb.Append('\n');
        return sb;
    }
}