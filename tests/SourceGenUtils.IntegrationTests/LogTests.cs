using System.Reflection;
using Bogus;
using Hertzole.SourceGen;

namespace Hertzole.SourceGenUtils.IntegrationTests;

public class LogTests
{
    private static readonly string logPath =
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), Assembly.GetCallingAssembly().GetName().Name + ".log"));

    private readonly Faker faker = new Faker();

    [SetUp]
    public void Setup()
    {
        Log.ClearLogs();
    }

    [Test]
    public void WritesInfo()
    {
        // Assert
        string? message = faker.Lorem.Sentence();

        // Act
        Log.Info(message);

        // Assert
        AssertLogMessage(message, "INFO");
    }

    [Test]
    public void WritesWarning()
    {
        // Assert
        string? message = faker.Lorem.Sentence();

        // Act
        Log.Warning(message);

        // Assert
        AssertLogMessage(message, "WARNING");
    }

    [Test]
    public void WritesError()
    {
        // Assert
        string? message = faker.Lorem.Sentence();

        // Act
        Log.Error(message);

        // Assert
        AssertLogMessage(message, "ERROR");
    }

    [Test]
    public void ClearLogs()
    {
        // Assert
        string[]? messages = faker.Lorem.Words(10);
        for (int i = 0; i < messages.Length; i++)
        {
            Log.Info(messages[i]);
        }

        // Act
        Log.ClearLogs();

        // Assert
        Assert.That(File.ReadAllText(logPath), Is.Empty);
    }

    private static void AssertLogMessage(string message, string prefix)
    {
        if (!File.Exists(logPath))
        {
            Assert.Fail("Log file does not exist");
            return;
        }

        string[] lines = File.ReadAllLines(logPath);

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