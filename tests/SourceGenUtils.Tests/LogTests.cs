using System;
using System.IO;
using System.Reflection;
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
    [TestCase("Info(object)", "INFO")]
    [TestCase("Warning(object)", "WARNING")]
    [TestCase("Error(object)", "ERROR")]
    public void WriteTest(string methodName, string prefix)
    {
        // Arrange
        Type type = CompileGeneratedType("Log", $"Log.{methodName}");
        MethodInfo method = GetMethod(type, GetMethodNameWithoutArgs(methodName).ToString(), BindingFlags.Static | BindingFlags.Public);
        string logsPath = GetField(type, "path", BindingFlags.NonPublic | BindingFlags.Static).GetValue<string>();
        string message = Fake.Lorem.Sentence();

        // Act
        method.InvokeStatic(message);

        // Assert
        AssertLogMessage(logsPath, message, prefix);
    }

    [Test]
    public void ClearLogs()
    {
        // Arrange
        string[] messages = Fake.Lorem.Paragraphs().Split("\n\n");
        Type type = CompileGeneratedType("Log", "Log.ClearLogs()", "Log.Info(object)");
        MethodInfo clearMethod = GetMethod(type, "ClearLogs", BindingFlags.Static | BindingFlags.Public);
        string logsPath = GetField(type, "path", BindingFlags.NonPublic | BindingFlags.Static).GetValue<string>();
        MethodInfo writeMethod = GetMethod(type, "Info", BindingFlags.Public | BindingFlags.Static);

        // Act
        for (int i = 0; i < messages.Length; i++)
        {
            writeMethod.InvokeStatic(messages[i]);
        }

        bool emptyAfterWrite = string.IsNullOrWhiteSpace(File.ReadAllText(logsPath));

        clearMethod.InvokeStatic();

        // Assert
        Assert.That(emptyAfterWrite, Is.False, "No logs were written.");
        Assert.That(File.ReadAllText(logsPath), Is.Empty, "Logs were not cleared.");
    }

    [Test]
    public void CanWriteMultipleLogs()
    {
        // Arrange
        string[] messages = Fake.Lorem.Paragraphs().Split("\n\n");
        Type type = CompileGeneratedType("Log", "Log.Info(object)");
        string logsPath = GetField(type, "path", BindingFlags.NonPublic | BindingFlags.Static).GetValue<string>();
        MethodInfo writeMethod = GetMethod(type, "Info", BindingFlags.Public | BindingFlags.Static);

        // Act
        for (int i = 0; i < messages.Length; i++)
        {
            writeMethod.InvokeStatic(messages[i]);
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
}