using System;
using System.Collections;
using Hertzole.SourceGen.Wrappers;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

public class VariableNamesTests : GeneratorTests
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

    public static IEnumerable AppendGlobalPrefixCases
    {
        get
        {
            yield return new TestCaseData("global::MyType").Returns("global::MyType");
            yield return new TestCaseData("MyType").Returns("global::MyType");
            yield return new TestCaseData("GLOBAL::MyType").Returns("global::GLOBAL::MyType");
            yield return new TestCaseData("global::__MyType").Returns("global::__MyType");
            yield return new TestCaseData("__MyType").Returns("global::__MyType");
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

    public static IEnumerable GetNameWithGlobalPrefixLengthCases
    {
        get
        {
            yield return new TestCaseData("global::MyType").Returns("global::MyType".Length);
            yield return new TestCaseData("MyType").Returns("global::MyType".Length);
            yield return new TestCaseData("GLOBAL::MyType").Returns("global::GLOBAL::MyType".Length);
            yield return new TestCaseData("global::__MyType").Returns("global::__MyType".Length);
            yield return new TestCaseData("__MyType").Returns("global::__MyType".Length);
        }
    }

    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "VariableNames";
    }

    [Test]
    [TestCaseSource(nameof(NicifyVariableNamesCases))]
    public string NicifyVariableName_Span(string value)
    {
        // Arrange
        VariableNames wrapper =
            GetWrapper("VariableNames.NicifyVariableName(ReadOnlySpan<char>.Empty, Span<char>.Empty); VariableNames.GetNiceNameLength(\"\");");

        Span<char> destination = stackalloc char[wrapper.GetNiceNameLength(value)];

        // Act
        wrapper.NicifyVariableName(value, destination);
        return new string(destination);
    }

    [Test]
    [TestCaseSource(nameof(NicifyVariableNamesCases))]
    public string NicifyVariableName_String(string value)
    {
        // Arrange
        VariableNames wrapper = GetWrapper("VariableNames.NicifyVariableName(\"test\");");

        // Act
        return wrapper.NicifyVariableName(value);
    }

    [Test]
    [TestCaseSource(nameof(NicifyVariableNamesCases))]
    public string NicifyVariableName_ArrayBuilder(string value)
    {
        // Arrange
        Type type = CompileGeneratedTypeByUsing("VariableNames",
            "var builder = new ArrayBuilder<char>(); VariableNames.NicifyVariableName(\"test\", builder); builder.ToString();");

        VariableNames wrapper = new VariableNames(type);
        using ArrayBuilder<char> arrayBuilder = new ArrayBuilder<char>(type.Assembly);
        int expectedWritten = "PlayerHealth".Length;

        // Act
        int written = wrapper.NicifyVariableName(value, arrayBuilder);

        // Assert
        Assert.That(written, Is.EqualTo(expectedWritten));
        return arrayBuilder.ToString();
    }

    [Test]
    [TestCaseSource(nameof(RemovePrefixCases))]
    public string RemovePrefix_Span(string value)
    {
        // Arrange
        VariableNames wrapper = GetWrapper("VariableNames.RemovePrefix(ReadOnlySpan<char>.Empty, Span<char>.Empty);");
        Span<char> destination = stackalloc char[value.Length];

        // Act
        int written = wrapper.RemovePrefix(value.AsSpan(), destination);

        // Assert
        return new string(destination.Slice(0, written));
    }

    [Test]
    [TestCaseSource(nameof(RemovePrefixCases))]
    public string RemovePrefix_String(string value)
    {
        // Arrange
        VariableNames wrapper = GetWrapper("VariableNames.RemovePrefix(\"test\");");

        // Act
        return wrapper.RemovePrefix(value);
    }

    [Test]
    [TestCaseSource(nameof(RemovePrefixCases))]
    public string RemovePrefix_ArrayBuilder(string value)
    {
        // Arrange
        Type type = CompileGeneratedTypeByUsing("VariableNames",
            "var builder = new ArrayBuilder<char>(); VariableNames.RemovePrefix(\"test\", builder); builder.ToString();");

        VariableNames wrapper = new VariableNames(type);
        ArrayBuilder<char> arrayBuilder = new ArrayBuilder<char>(type.Assembly);

        // Act
        wrapper.RemovePrefix(value, arrayBuilder);

        // Assert
        return arrayBuilder.ToString();
    }

    [Test]
    [TestCaseSource(nameof(UppercaseStartCases))]
    public string UppercaseStart_Span(string value)
    {
        // Arrange
        VariableNames wrapper = GetWrapper("VariableNames.UppercaseStart(ReadOnlySpan<char>.Empty, Span<char>.Empty);");
        Span<char> destination = stackalloc char[value.Length];

        // Act
        wrapper.UppercaseStart(value.AsSpan(), destination);

        // Assert
        return new string(destination);
    }

    [Test]
    [TestCaseSource(nameof(UppercaseStartCases))]
    public string UppercaseStart_String(string value)
    {
        // Arrange
        VariableNames wrapper = GetWrapper("VariableNames.UppercaseStart(\"\");");

        // Act
        return wrapper.UppercaseStart(value);
    }

    [Test]
    [TestCaseSource(nameof(UppercaseStartCases))]
    public string UppercaseStart_ArrayBuilder(string value)
    {
        // Arrange
        Type type = CompileGeneratedTypeByUsing("VariableNames",
            "var builder = new ArrayBuilder<char>(); VariableNames.UppercaseStart(\"\", builder); builder.ToString();");

        VariableNames wrapper = new VariableNames(type);
        using ArrayBuilder<char> arrayBuilder = new ArrayBuilder<char>(type.Assembly);

        // Act
        wrapper.UppercaseStart(value, arrayBuilder);

        // Assert
        return arrayBuilder.ToString();
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
        // Arrange
        VariableNames wrapper = GetWrapper("VariableNames.StartsWithOn(\"\");");

        // Act
        return wrapper.StartsWithOn(value);
    }

    [Test]
    [TestCaseSource(typeof(VariableNamesTests), nameof(GetNiceNameLengthCases))]
    public int GetNiceNameLength_String(string value)
    {
        // Arrange
        VariableNames wrapper = GetWrapper("VariableNames.GetNiceNameLength(\"\");");

        // Act
        return wrapper.GetNiceNameLength(value);
    }

    [Test]
    [TestCaseSource(nameof(AppendGlobalPrefixCases))]
    public string AppendGlobalPrefix_Span(string value)
    {
        // Arrange
        VariableNames wrapper = GetWrapper("VariableNames.AppendGlobalPrefix(ReadOnlySpan<char>.Empty, Span<char>.Empty);");
        Span<char> destination = stackalloc char[value.Length + 8];

        // Act
        int written = wrapper.AppendGlobalPrefix(value, destination);

        // Assert
        return new string(destination.Slice(0, written));
    }

    [Test]
    [TestCaseSource(nameof(AppendGlobalPrefixCases))]
    public string AppendGlobalPrefix_String(string value)
    {
        // Arrange
        VariableNames wrapper = GetWrapper("VariableNames.AppendGlobalPrefix(\"\");");

        // Act
        return wrapper.AppendGlobalPrefix(value);
    }

    [Test]
    [TestCaseSource(nameof(AppendGlobalPrefixCases))]
    public string AppendGlobalPrefix_ArrayBuilder(string value)
    {
        // Arrange
        Type type = CompileGeneratedTypeByUsing("VariableNames",
            "var builder = new ArrayBuilder<char>(); VariableNames.AppendGlobalPrefix(\"\", builder); builder.ToString();");

        VariableNames wrapper = new VariableNames(type);
        using ArrayBuilder<char> arrayBuilder = new ArrayBuilder<char>(type.Assembly);
        int expectedWritten = value.StartsWith("global::") ? value.Length : value.Length + "global::".Length;

        // Act
        int written = wrapper.AppendGlobalPrefix(value, arrayBuilder);

        // Assert
        Assert.That(written, Is.EqualTo(expectedWritten));
        return arrayBuilder.ToString();
    }

    [Test]
    [TestCaseSource(typeof(VariableNamesTests), nameof(GetNameWithGlobalPrefixLengthCases))]
    public int GetNameWithGlobalPrefixLength_String(string value)
    {
        // Arrange
        VariableNames wrapper = GetWrapper("VariableNames.GetNameWithGlobalPrefixLength(\"\");");

        // Act
        return wrapper.GetNameWithGlobalPrefixLength(value);
    }

    private static VariableNames GetWrapper(string useMethods)
    {
        return new VariableNames(CompileGeneratedTypeByUsing("VariableNames", useMethods));
    }
}