using System;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

internal partial class CodeWriterTests
{
    [Test]
    [TestCaseSource(nameof(AppendCases))]
    public void AppendLine<T>(T value)
    {
        CreateAppendTest(AppendType.AppendLine, value);
    }

    [Test]
    [TestCaseSource(nameof(AppendFormatCases))]
    public void AppendLineFormat<T>(T value) where T : IFormattable
    {
        CreateAppendFormatTest(AppendType.AppendLine, value);
    }
}