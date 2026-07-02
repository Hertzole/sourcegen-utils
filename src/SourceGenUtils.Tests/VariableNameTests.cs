using NUnit.Framework;

namespace SourceGenUtils.Tests;

internal class VariableNameTests : GeneratorTests
{
    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "VariableNames";
    }

    /// <inheritdoc />
    protected override string GetTypeOutline()
    {
        return """
               internal static partial class VariableNames
               {
               }
               """;
    }

    /// <inheritdoc />
    protected override string[] GetShellMethods()
    {
        return
        [
            "NicifyVariableName(System.ReadOnlySpan<char>, System.Span<char>)",
            "NicifyVariableName(string)",
            "RemovePrefix(System.ReadOnlySpan<char>, System.Span<char>)",
            "RemovePrefix(string)",
            "UppercaseStart(System.ReadOnlySpan<char>, System.Span<char>)",
            "UppercaseStart(string)",
            "StartsWithOn(System.ReadOnlySpan<char>)"
        ];
    }

    [Test]
    public void Call_NicifyVariableName()
    {
        string[] expectedMethods =
        [
            "VariableNames.NicifyVariableName(System.ReadOnlySpan<char>, System.Span<char>)",
            "VariableNames.RemovePrefix(System.ReadOnlySpan<char>, System.Span<char>)",
            "VariableNames.UppercaseStart(System.ReadOnlySpan<char>, System.Span<char>)"
        ];

        CallTest(writer => { writer.AppendLine("VariableNames.NicifyVariableName(\"hello\", new char[64]);"); }, expectedMethods);
    }

    [Test]
    public void Call_RemovePrefix()
    {
        string[] expectedMethods =
        [
            "VariableNames.RemovePrefix(System.ReadOnlySpan<char>, System.Span<char>)"
        ];

        CallTest(writer => { writer.AppendLine("VariableNames.RemovePrefix(\"hello\", new char[64]);"); }, expectedMethods);
    }

    [Test]
    public void Call_UppercaseStart()
    {
        string[] expectedMethods =
        [
            "VariableNames.UppercaseStart(System.ReadOnlySpan<char>, System.Span<char>)"
        ];

        CallTest(writer => { writer.AppendLine("VariableNames.UppercaseStart(\"hello\", new char[64]);"); }, expectedMethods);
    }

    [Test]
    public void Call_StartsWithOn()
    {
        string[] expectedMethods =
        [
            "VariableNames.StartsWithOn(System.ReadOnlySpan<char>)"
        ];

        CallTest(writer => { writer.AppendLine("VariableNames.StartsWithOn(\"hello\");"); }, expectedMethods);
    }

    [Test]
    public void NicifyVariableName_Content()
    {
        // Arrange
        string content = GetMethodContent("VariableNames.NicifyVariableName(System.ReadOnlySpan<char>, System.Span<char>)");
        const string expected = """
                                public static partial int NicifyVariableName(global::System.ReadOnlySpan<char> value, global::System.Span<char> destination)
                                {
                                    int written = RemovePrefix(value, destination);
                                    UppercaseStart(destination.Slice(0, written), destination);
                                    return written;
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void RemovePrefix_Content()
    {
        // Arrange
        string content = GetMethodContent("VariableNames.RemovePrefix(System.ReadOnlySpan<char>, System.Span<char>)");
        const string expected = """
                                public static partial int RemovePrefix(global::System.ReadOnlySpan<char> value, global::System.Span<char> destination)
                                {
                                    // Check for prefixes like 'm_'.
                                    if (value.Length > 2 && value[1] == '_')
                                    {
                                        value.Slice(2).CopyTo(destination);
                                        return value.Length - 2;
                                    }

                                    // Check for names that start with '_' or 'k' (konstants).
                                    if (value.Length > 1 && (value[0] == '_' || value[0] == 'k'))
                                    {
                                        value.Slice(1).CopyTo(destination);
                                        return value.Length - 1;
                                    }

                                    value.CopyTo(destination);
                                    return value.Length;
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void UppercaseStart_Content()
    {
        // Arrange
        string content = GetMethodContent("VariableNames.UppercaseStart(System.ReadOnlySpan<char>, System.Span<char>)");
        const string expected = """
                                public static partial void UppercaseStart(global::System.ReadOnlySpan<char> value, global::System.Span<char> destination)
                                {
                                    if (value.Length == 0)
                                    {
                                        // Empty string.
                                        value.CopyTo(destination);
                                        return;
                                    }

                                    if (value[0] == char.ToUpperInvariant(value[0]))
                                    {
                                        // Already uppercase.
                                        value.CopyTo(destination);
                                        return;
                                    }

                                    value.CopyTo(destination);
                                    destination[0] = char.ToUpperInvariant(value[0]);
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void StartsWithOn_Content()
    {
        // Arrange
        string content = GetMethodContent("VariableNames.StartsWithOn(System.ReadOnlySpan<char>)");
        const string expected = """
                                public static partial bool StartsWithOn(global::System.ReadOnlySpan<char> value)
                                {
                                    // Check if the value starts with 'on' or 'On' and that the third character is uppercase.
                                    // Checking the third character ensures it doesn't match words like "only" or "once".
                                    return value.Length >= 3 && (value[0] == 'o' || value[0] == 'O') && value[1] == 'n' && char.IsUpper(value[2]);
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void NicifyVariableName_Content_NotCalled()
    {
        // Arrange
        const string expected = """
                                public static partial int NicifyVariableName(global::System.ReadOnlySpan<char> value, global::System.Span<char> destination)
                                {
                                    return 0;
                                }
                                """;

        // Assert
        EmptyContentTest("VariableNames.NicifyVariableName(System.ReadOnlySpan<char>, System.Span<char>)", expected);
    }

    [Test]
    public void RemovePrefix_Content_NotCalled()
    {
        // Arrange
        const string expected = """
                                public static partial int RemovePrefix(global::System.ReadOnlySpan<char> value, global::System.Span<char> destination)
                                {
                                    return 0;
                                }
                                """;

        // Assert
        EmptyContentTest("VariableNames.RemovePrefix(System.ReadOnlySpan<char>, System.Span<char>)", expected);
    }

    [Test]
    public void UppercaseStart_Content_NotCalled()
    {
        // Arrange
        const string expected = """
                                public static partial void UppercaseStart(global::System.ReadOnlySpan<char> value, global::System.Span<char> destination)
                                {
                                }
                                """;

        // Assert
        EmptyContentTest("VariableNames.UppercaseStart(System.ReadOnlySpan<char>, System.Span<char>)", expected);
    }

    [Test]
    public void StartsWithOn_Content_NotCalled()
    {
        // Arrange
        const string expected = """
                                public static partial bool StartsWithOn(global::System.ReadOnlySpan<char> value)
                                {
                                    return false;
                                }
                                """;

        // Assert
        EmptyContentTest("VariableNames.StartsWithOn(System.ReadOnlySpan<char>)", expected);
    }
}