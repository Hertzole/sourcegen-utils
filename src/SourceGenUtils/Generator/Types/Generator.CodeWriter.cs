using Hertzole.SourceGenUtils.Helpers;

namespace Hertzole.SourceGenUtils;

partial class Generator
{
    private static TypeSource CreateCodeWriter()
    {
        return CodeWriterGenerator.CreateCodeWriter();
    }
}