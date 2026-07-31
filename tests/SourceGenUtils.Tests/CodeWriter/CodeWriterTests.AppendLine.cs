using System;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

internal partial class CodeWriterTests
{
    [Test]
    [TestCaseSource(nameof(AppendCases))]
    public void AppendLine<T>(T value, bool isUnsafe)
    {
        CreateAppendTest(AppendType.AppendLine, isUnsafe, value);
    }

    [Test]
    [TestCaseSource(nameof(AppendFormatCases))]
    public void AppendLineFormat<T>(T value, bool isUnsafe) where T : IFormattable
    {
        CreateAppendFormatTest(AppendType.AppendLine, isUnsafe, value);
    }
}