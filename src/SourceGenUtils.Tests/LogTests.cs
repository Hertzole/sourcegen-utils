using Hertzole.SourceGenUtils;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

internal class LogTests : GeneratorTests
{
    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "Log";
    }

    [Test]
    public void Call_Info()
    {
        // Arrange
        string source = GenerateCall(writer => { writer.AppendLine("Log.Info(\"hello\");"); });
        string expected = GetTypeContent("Log.Info(object)", "Log.Write(string)");

        // Act
        GeneratorDriverRunResult result = AssertGeneratedOutput<Generator>(source);

        // Assert
        AssertGenerateTypeHasContent(expected, result);
    }

    [Test]
    public void Call_Warning()
    {
        // Arrange
        string source = GenerateCall(writer => { writer.AppendLine("Log.Warning(\"hello\");"); });
        string expected = GetTypeContent("Log.Warning(object)", "Log.Write(string)");

        // Act
        GeneratorDriverRunResult result = AssertGeneratedOutput<Generator>(source);

        // Assert
        AssertGenerateTypeHasContent(expected, result);
    }

    [Test]
    public void Call_Error()
    {
        // Arrange
        string source = GenerateCall(writer => { writer.AppendLine("Log.Error(\"hello\");"); });
        string expected = GetTypeContent("Log.Error(object)", "Log.Write(string)");

        // Act
        GeneratorDriverRunResult result = AssertGeneratedOutput<Generator>(source);

        // Assert
        AssertGenerateTypeHasContent(expected, result);
    }

    [Test]
    public void Info_Content()
    {
        // Arrange
        string content = GetMethodContent("Log.Info(object)");
        const string expected = """
                                [global::System.Diagnostics.Conditional("DEBUG")]
                                public static partial void Info(object message)
                                {
                                #if DEBUG
                                    Write($"[INFO] {message}");
                                #endif
                                }

                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Warning_Content()
    {
        // Arrange
        string content = GetMethodContent("Log.Warning(object)");
        const string expected = """
                                [global::System.Diagnostics.Conditional("DEBUG")]
                                public static partial void Warning(object message)
                                {
                                #if DEBUG
                                    Write($"[WARNING] {message}");
                                #endif
                                }

                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Error_Content()
    {
        // Arrange
        string content = GetMethodContent("Log.Error(object)");
        const string expected = """
                                [global::System.Diagnostics.Conditional("DEBUG")]
                                public static partial void Error(object message)
                                {
                                #if DEBUG
                                    Write($"[ERROR] {message}");
                                #endif
                                }

                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void Write_Content()
    {
        // Arrange
        string content = GetMethodContent("Log.Write(string)");
        const string expected = """
                                #if DEBUG
                                private static void Write(string message)
                                {
                                    if (!isInitialized)
                                    {
                                        isInitialized = true;
                                        global::System.IO.File.WriteAllText(path, string.Empty);
                                    }

                                    using (global::System.IO.FileStream stream = global::System.IO.File.Open(path, global::System.IO.FileMode.Append, global::System.IO.FileAccess.Write, global::System.IO.FileShare.Read))
                                    {
                                        byte[] bytes = global::System.Text.Encoding.UTF8.GetBytes($"[{System.DateTimeOffset.Now:HH:mm:ss.fff}]: {message}{System.Environment.NewLine}");
                                        stream.Write(bytes, 0, bytes.Length);
                                    }
                                }
                                #endif

                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }
}