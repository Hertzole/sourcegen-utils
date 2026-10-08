#if DEBUG
using System;
using System.IO;
using Hertzole.SourceGen.Wrappers;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

[NonParallelizable]
internal class LogTests : GeneratorTests
{
    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "Log";
    }

    [Test]
    [TestCase("Info", "INFO")]
    [TestCase("Warning", "WARNING")]
    [TestCase("Error", "ERROR")]
    public void WriteTest(string methodName, string prefix)
    {
        // Arrange
        string message = Fake.Lorem.Sentence();
        Log log = GetWrapper($"{NAMESPACE}.Log.{methodName}(new object());");
        string logsPath = log.path;

        // Act
        switch (methodName)
        {
            case "Info":
                log.Info(message);
                break;
            case "Warning":
                log.Warning(message);
                break;
            case "Error":
                log.Error(message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(methodName), methodName, null);
        }

        // Assert
        AssertLogMessage(logsPath, message, prefix);
    }

    [Test]
    public void ClearLogs()
    {
        // Arrange
        string[] messages = Fake.Lorem.Paragraphs().Split("\n\n");
        Log log = GetWrapper($"{NAMESPACE}.Log.ClearLogs(); {NAMESPACE}.Log.Info(new object());");
        string logsPath = log.path;

        // Act
        for (int i = 0; i < messages.Length; i++)
        {
            log.Info(messages[i]);
        }

        bool emptyAfterWrite = string.IsNullOrWhiteSpace(File.ReadAllText(logsPath));

        log.ClearLogs();

        // Assert
        Assert.That(emptyAfterWrite, Is.False, "No logs were written.");
        Assert.That(File.ReadAllText(logsPath), Is.Empty, "Logs were not cleared.");
    }

    [Test]
    public void CanWriteMultipleLogs()
    {
        // Arrange
        string[] messages = Fake.Lorem.Paragraphs().Split("\n\n");
        Log log = GetWrapper($"{NAMESPACE}.Log.Info(new object());");
        string logsPath = log.path;

        // Act
        for (int i = 0; i < messages.Length; i++)
        {
            log.Info(messages[i]);
        }

        // Assert
        string content = File.ReadAllText(logsPath);
        Assert.That(content, Is.Not.Empty, "Logs were not written.");
        for (int i = 0; i < messages.Length; i++)
        {
            Assert.That(content, Contains.Substring(messages[i]));
        }
    }

    private static void AssertLogMessage(string path, string message, string prefix)
    {
        if (!File.Exists(path))
        {
            Assert.Fail("Log file does not exist.");
            return;
        }

        string[] lines = File.ReadAllLines(path);

        Assert.That(lines, Has.Length.EqualTo(1), "There should only be one line.");

        ReadOnlySpan<char> line = lines[0].AsSpan();

        int timestampEndIndex = line.IndexOf(']');

        if (timestampEndIndex == -1)
        {
            Assert.Fail("Timestamp end not found");
            return;
        }

        string withoutTimestamp = line.Slice(timestampEndIndex + 1).ToString();

        Assert.That(withoutTimestamp, Does.Contain($"[{prefix}]"));
        Assert.That(withoutTimestamp, Does.Contain(message));
    }

    private static Log GetWrapper(string useMethods)
    {
        return new Log(CompileGeneratedTypeByUsing("Log", useMethods));
    }
}
#endif