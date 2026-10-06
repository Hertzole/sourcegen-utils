#if DEBUG
using System;
using System.IO;
using System.Reflection;
using Hertzole.SourceGenUtils;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

[NonParallelizable]
internal class LogTests : GeneratorTests
{
    private class LogWrapper
    {
        private readonly Type type;

        private MethodInfo InfoMethod
        {
            get
            {
                if (field == null)
                {
                    field = type.GetMethod("Info", BindingFlags.Public | BindingFlags.Static);
                }

                if (field == null)
                {
                    throw new Exception("Could not find Info method.");
                }

                return field;
            }
        }

        private MethodInfo WarningMethod
        {
            get
            {
                if (field == null)
                {
                    field = type.GetMethod("Warning", BindingFlags.Public | BindingFlags.Static);
                }

                if (field == null)
                {
                    throw new Exception("Could not find Warning method.");
                }

                return field;
            }
        }

        private MethodInfo ErrorMethod
        {
            get
            {
                if (field == null)
                {
                    field = type.GetMethod("Error", BindingFlags.Public | BindingFlags.Static);
                }

                if (field == null)
                {
                    throw new Exception("Could not find Error method.");
                }

                return field;
            }
        }

        private MethodInfo ClearLogsMethod
        {
            get
            {
                if (field == null)
                {
                    field = type.GetMethod("ClearLogs", BindingFlags.Static | BindingFlags.Public);
                }

                if (field == null)
                {
                    throw new Exception("Could not find ClearLogs method.");
                }

                return field;
            }
        }

        public FieldInfo Path
        {
            get
            {
                if (field == null)
                {
                    field = type.GetField("path", BindingFlags.NonPublic | BindingFlags.Static);
                }

                if (field == null)
                {
                    throw new Exception("Could not find path field.");
                }

                return field;
            }
        }

        public LogWrapper(Type type)
        {
            this.type = type;
        }

        public void Info(object message)
        {
            InfoMethod.InvokeStatic(message);
        }

        public void Warning(object message)
        {
            WarningMethod.InvokeStatic(message);
        }

        public void Error(object message)
        {
            ErrorMethod.InvokeStatic(message);
        }

        public void ClearLogs()
        {
            ClearLogsMethod.InvokeStatic();
        }
    }

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
        LogWrapper log = new LogWrapper(CompileGeneratedTypeByUsing("Log", $"{Generator.NAMESPACE}.Log.{methodName}(new object());"));
        string logsPath = log.Path.GetValue<string>();

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
        LogWrapper log = new LogWrapper(CompileGeneratedTypeByUsing("Log", $"{NAMESPACE}.Log.ClearLogs(); {NAMESPACE}.Log.Info(new object());"));
        string logsPath = log.Path.GetValue<string>();

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
        LogWrapper log = new LogWrapper(CompileGeneratedTypeByUsing("Log", $"{NAMESPACE}.Log.Info(new object());"));
        string logsPath = log.Path.GetValue<string>();

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
}
#endif