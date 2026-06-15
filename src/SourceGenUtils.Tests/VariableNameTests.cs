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
            "NicifyVariableName(System.ReadOnlySpan<char>)",
            "RemovePrefix(System.ReadOnlySpan<char>)",
            "UppercaseStart(System.ReadOnlySpan<char>)",
            "StartsWithOn(System.ReadOnlySpan<char>)"
        ];
    }

    [Test]
    public void Call_NicifyVariableName()
    {
        string[] expectedMethods =
        [
            "VariableNames.NicifyVariableName(System.ReadOnlySpan<char>)",
            "VariableNames.RemovePrefix(System.ReadOnlySpan<char>)",
            "VariableNames.UppercaseStart(System.ReadOnlySpan<char>)"
        ];

        CallTest(writer => { writer.AppendLine("VariableNames.NicifyVariableName(\"hello\");"); }, expectedMethods);
    }

    [Test]
    public void Call_RemovePrefix()
    {
        string[] expectedMethods =
        [
            "VariableNames.RemovePrefix(System.ReadOnlySpan<char>)"
        ];

        CallTest(writer => { writer.AppendLine("VariableNames.RemovePrefix(\"hello\");"); }, expectedMethods);
    }

    [Test]
    public void Call_UppercaseStart()
    {
        string[] expectedMethods =
        [
            "VariableNames.UppercaseStart(System.ReadOnlySpan<char>)"
        ];

        CallTest(writer => { writer.AppendLine("VariableNames.UppercaseStart(\"hello\");"); }, expectedMethods);
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
        string content = GetMethodContent("VariableNames.NicifyVariableName(System.ReadOnlySpan<char>)");
        const string expected = """
                                public static partial global::System.ReadOnlySpan<char> NicifyVariableName(global::System.ReadOnlySpan<char> value)
                                {
                                    value = RemovePrefix(value);
                                    value = UppercaseStart(value);
                                    return value;
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void RemovePrefix_Content()
    {
        // Arrange
        string content = GetMethodContent("VariableNames.RemovePrefix(System.ReadOnlySpan<char>)");
        const string expected = """
                                public static partial global::System.ReadOnlySpan<char> RemovePrefix(global::System.ReadOnlySpan<char> value)
                                {
                                    // Check for prefixes like 'm_'.
                                    if (value.Length > 2 && value[1] == '_')
                                    {
                                        return value.Slice(2);
                                    }

                                    // Check for names that start with '_' or 'k' (konstants).
                                    if (value.Length > 1 && (value[0] == '_' || value[0] == 'k'))
                                    {
                                        return value.Slice(1);
                                    }

                                    return value;
                                }
                                """;

        // Assert
        Assert.That(content, Is.EqualTo(expected));
    }

    [Test]
    public void UppercaseStart_Content()
    {
        // Arrange
        string content = GetMethodContent("VariableNames.UppercaseStart(System.ReadOnlySpan<char>)");
        const string expected = """
                                public static partial global::System.ReadOnlySpan<char> UppercaseStart(global::System.ReadOnlySpan<char> value)
                                {
                                    if (value.Length == 0)
                                    {
                                        return value;
                                    }

                                    if (value[0] == char.ToUpperInvariant(value[0]))
                                    {
                                        // Already uppercase.
                                        return value;
                                    }

                                    char[] newValue = global::System.Buffers.ArrayPool<char>.Shared.Rent(value.Length);
                                    try
                                    {
                                        value.CopyTo(newValue);
                                        newValue[0] = char.ToUpperInvariant(value[0]);
                                        return new System.ReadOnlySpan<char>(newValue, 0, value.Length);
                                    }
                                    finally
                                    {
                                        global::System.Buffers.ArrayPool<char>.Shared.Return(newValue);
                                    }
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
                                public static partial global::System.ReadOnlySpan<char> NicifyVariableName(global::System.ReadOnlySpan<char> value)
                                {
                                    return value;
                                }
                                """;

        // Assert
        EmptyContentTest("VariableNames.NicifyVariableName(System.ReadOnlySpan<char>)", expected);
    }

    [Test]
    public void RemovePrefix_Content_NotCalled()
    {
        // Arrange
        const string expected = """
                                public static partial global::System.ReadOnlySpan<char> RemovePrefix(global::System.ReadOnlySpan<char> value)
                                {
                                    return value;
                                }
                                """;

        // Assert
        EmptyContentTest("VariableNames.RemovePrefix(System.ReadOnlySpan<char>)", expected);
    }

    [Test]
    public void UppercaseStart_Content_NotCalled()
    {
        // Arrange
        const string expected = """
                                public static partial global::System.ReadOnlySpan<char> UppercaseStart(global::System.ReadOnlySpan<char> value)
                                {
                                    return value;
                                }
                                """;

        // Assert
        EmptyContentTest("VariableNames.UppercaseStart(System.ReadOnlySpan<char>)", expected);
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