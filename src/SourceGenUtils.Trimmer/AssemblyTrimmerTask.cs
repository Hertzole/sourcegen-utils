using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace SourceGenUtils.Trimmer;

public class AssemblyTrimmerTask : Task
{
    [Required]
    public string AssemblyPath { get; set; } = string.Empty;

    public bool RemoveGeneratedCodeAttribute { get; set; } = true;
    public bool RemoveCodeCoverageAttribute { get; set; } = true;

    /// <inheritdoc />
    public override bool Execute()
    {
        AssemblyTrimmer trimmer = new AssemblyTrimmer
        {
            RemoveGeneratedCodeAttribute = RemoveGeneratedCodeAttribute,
            RemoveCodeCoverageAttribute = RemoveCodeCoverageAttribute
        };

        return trimmer.Trim(AssemblyPath);
    }
}