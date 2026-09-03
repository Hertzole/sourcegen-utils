using System;

namespace Hertzole.SourceGenUtils.Data;

[Flags]
internal enum AssemblyFeatures : byte
{
    None = 0,
    RecordSupport = 1 << 0,
    RequiredSupport = 1 << 1,
    All = RecordSupport | RequiredSupport
}