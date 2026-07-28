using System.Collections;
using Hertzole.SourceGen;

namespace Hertzole.SourceGenUtils.IntegrationTests;

public class VariableNamesTests
{
    private static readonly int niceNameLength = "PlayerHealth".Length;
    public static IEnumerable NicifyVariableNamesCases
    {
        get
        {
            yield return new TestCaseData("m_playerHealth").Returns("PlayerHealth");
            yield return new TestCaseData("m_PlayerHealth").Returns("PlayerHealth");
            yield return new TestCaseData("playerHealth").Returns("PlayerHealth");
            yield return new TestCaseData("PlayerHealth").Returns("PlayerHealth");
            yield return new TestCaseData("a_playerHealth").Returns("PlayerHealth");
            yield return new TestCaseData("a_PlayerHealth").Returns("PlayerHealth");
            yield return new TestCaseData("_playerHealth").Returns("PlayerHealth");
            yield return new TestCaseData("_PlayerHealth").Returns("PlayerHealth");
            yield return new TestCaseData("kPlayerHealth").Returns("PlayerHealth");
            yield return new TestCaseData("KPlayerHealth").Returns("PlayerHealth");
        }
    }

    public static IEnumerable RemovePrefixCases
    {
        get
        {
            yield return new TestCaseData("m_playerHealth").Returns("playerHealth");
            yield return new TestCaseData("m_PlayerHealth").Returns("PlayerHealth");
            yield return new TestCaseData("playerHealth").Returns("playerHealth");
            yield return new TestCaseData("PlayerHealth").Returns("PlayerHealth");
            yield return new TestCaseData("a_playerHealth").Returns("playerHealth");
            yield return new TestCaseData("a_PlayerHealth").Returns("PlayerHealth");
            yield return new TestCaseData("_playerHealth").Returns("playerHealth");
            yield return new TestCaseData("_PlayerHealth").Returns("PlayerHealth");
            yield return new TestCaseData("kPlayerHealth").Returns("PlayerHealth");
            yield return new TestCaseData("KPlayerHealth").Returns("PlayerHealth");
            yield return new TestCaseData("kplayerHealth").Returns("kplayerHealth");
            yield return new TestCaseData("KplayerHealth").Returns("KplayerHealth");
        }
    }

    public static IEnumerable UppercaseStartCases
    {
        get
        {
            yield return new TestCaseData("m_playerHealth").Returns("M_playerHealth");
            yield return new TestCaseData("m_PlayerHealth").Returns("M_PlayerHealth");
            yield return new TestCaseData("playerHealth").Returns("PlayerHealth");
            yield return new TestCaseData("PlayerHealth").Returns("PlayerHealth");
            yield return new TestCaseData("_playerHealth").Returns("_playerHealth");
            yield return new TestCaseData("_PlayerHealth").Returns("_PlayerHealth");
            yield return new TestCaseData("kPlayerHealth").Returns("KPlayerHealth");
            yield return new TestCaseData("KPlayerHealth").Returns("KPlayerHealth");
        }
    }

    public static IEnumerable GetNiceNameLengthCases
    {
        get
        {
            yield return new TestCaseData("m_playerHealth").Returns(niceNameLength);
            yield return new TestCaseData("m_PlayerHealth").Returns(niceNameLength);
            yield return new TestCaseData("playerHealth").Returns(niceNameLength);
            yield return new TestCaseData("PlayerHealth").Returns(niceNameLength);
            yield return new TestCaseData("_playerHealth").Returns(niceNameLength);
            yield return new TestCaseData("_PlayerHealth").Returns(niceNameLength);
            yield return new TestCaseData("kPlayerHealth").Returns(niceNameLength);
            yield return new TestCaseData("KPlayerHealth").Returns(niceNameLength);
        }
    }

    [Test]
    [TestCaseSource(typeof(VariableNamesTests), nameof(NicifyVariableNamesCases))]
    public string NicifyVariableName_Span(string value)
    {
        // Arrange
        int expectedWritten = "PlayerHealth".Length;
        char[] destination = new char[expectedWritten];

        // Act
        int written = VariableNames.NicifyVariableName(value, destination);

        // Assert
        Assert.That(written, Is.EqualTo(expectedWritten));
        return new string(destination, 0, written);
    }

    [Test]
    [TestCaseSource(typeof(VariableNamesTests), nameof(NicifyVariableNamesCases))]
    public string NicifyVariableName_ArrayBuilder(string value)
    {
        // Arrange
        using ArrayBuilder<char> builder = new ArrayBuilder<char>();
        int expectedWritten = "PlayerHealth".Length;

        // Act
        int written = VariableNames.NicifyVariableName(value, builder);

        // Assert
        Assert.That(written, Is.EqualTo(expectedWritten));
        return builder.ToString();
    }

    [Test]
    [TestCaseSource(typeof(VariableNamesTests), nameof(NicifyVariableNamesCases))]
    public string NicifyVariableName_String(string value)
    {
        // Act
        string result = VariableNames.NicifyVariableName(value);

        // Assert
        return result;
    }

    [Test]
    [TestCaseSource(typeof(VariableNamesTests), nameof(RemovePrefixCases))]
    public string RemovePrefix_Span(string value)
    {
        // Arrange
        char[] destination = new char[16];

        // Act
        int written = VariableNames.RemovePrefix(value, destination);

        // Assert
        return new string(destination, 0, written);
    }

    [Test]
    [TestCaseSource(typeof(VariableNamesTests), nameof(RemovePrefixCases))]
    public string RemovePrefix_ArrayBuilder(string value)
    {
        // Arrange
        using ArrayBuilder<char> builder = new ArrayBuilder<char>();

        // Act
        VariableNames.RemovePrefix(value, builder);

        // Assert
        return builder.ToString();
    }

    [Test]
    [TestCaseSource(typeof(VariableNamesTests), nameof(RemovePrefixCases))]
    public string RemovePrefix_String(string value)
    {
        // Act
        string result = VariableNames.RemovePrefix(value);

        // Assert
        return result;
    }

    [Test]
    [TestCaseSource(typeof(VariableNamesTests), nameof(UppercaseStartCases))]
    public string UppercaseStart_Span(string value)
    {
        // Arrange
        char[] destination = new char[16];

        // Act
        VariableNames.UppercaseStart(value, destination);

        // Assert
        return new string(destination, 0, value.Length);
    }

    [Test]
    [TestCaseSource(typeof(VariableNamesTests), nameof(UppercaseStartCases))]
    public string UppercaseStart_ArrayBuilder(string value)
    {
        // Arrange
        using ArrayBuilder<char> builder = new ArrayBuilder<char>();

        // Act
        VariableNames.UppercaseStart(value, builder);

        // Assert
        return builder.ToString();
    }

    [Test]
    [TestCaseSource(typeof(VariableNamesTests), nameof(UppercaseStartCases))]
    public string UppercaseStart_String(string value)
    {
        // Act
        string result = VariableNames.UppercaseStart(value);

        // Assert
        return result;
    }

    [Test]
    [TestCase("On", ExpectedResult = false)]
    [TestCase("on", ExpectedResult = false)]
    [TestCase("OnEvent", ExpectedResult = true)]
    [TestCase("onEvent", ExpectedResult = true)]
    [TestCase("Only", ExpectedResult = false)]
    [TestCase("only", ExpectedResult = false)]
    public bool StartsWithOn(string value)
    {
        return VariableNames.StartsWithOn(value);
    }

    [Test]
    [TestCaseSource(typeof(VariableNamesTests), nameof(GetNiceNameLengthCases))]
    public int GetNiceNameLength_Span(string value)
    {
        return VariableNames.GetNiceNameLength(value);
    }

    [Test]
    [TestCaseSource(typeof(VariableNamesTests), nameof(GetNiceNameLengthCases))]
    public int GetNiceNameLength_String(string value)
    {
        return VariableNames.GetNiceNameLength(value.AsSpan());
    }
}