using System;
using System.Collections;
using System.Reflection;
using Hertzole.SourceGenUtils;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

public class VariableNamesTests : GeneratorTests
{
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

    private static readonly int niceNameLength = "PlayerHealth".Length;
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

    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "VariableNames";
    }

    [Test]
    [TestCaseSource(nameof(NicifyVariableNamesCases))]
    public string NicifyVariableName_String(string value)
    {
        // Arrange
        Type type = CompileVariableNames("NicifyVariableName(string)");
        MethodInfo method = GetMethod(type, "NicifyVariableName", BindingFlags.Public | BindingFlags.Static, typeof(string));

        // Act
        return method.InvokeStatic<string>(value);
    }

    [Test]
    [TestCaseSource(nameof(NicifyVariableNamesCases))]
    public string NicifyVariableName_ArrayBuilder(string value)
    {
        // Arrange
        Type type = CompileVariableNames($"NicifyVariableName(string, {Generator.ARRAY_BUILDER}<char>)", $"{Generator.ARRAY_BUILDER}.ArrayBuilder()",
            $"{Generator.ARRAY_BUILDER}.ToString()");

        Type arrayBuilderType = type.Assembly.GetType($"{Generator.NAMESPACE}.ArrayBuilder`1", true)!.MakeGenericType(typeof(char));
        object arrayBuilderInstance = CreateInstance(arrayBuilderType);
        MethodInfo method = GetMethod(type, "NicifyVariableName", BindingFlags.Public | BindingFlags.Static, typeof(string), arrayBuilderType);
        MethodInfo arrayBuilderToString = GetMethod(arrayBuilderType, "ToString", BindingFlags.Public | BindingFlags.Instance);
        int expectedWritten = "PlayerHealth".Length;

        // Act
        int written = method.InvokeStatic<int>(value, arrayBuilderInstance);

        // Assert
        Assert.That(written, Is.EqualTo(expectedWritten));
        return arrayBuilderToString.InvokeInstance<string>(arrayBuilderInstance);
    }

    [Test]
    [TestCaseSource(nameof(RemovePrefixCases))]
    public string RemovePrefix_String(string value)
    {
        // Arrange
        Type type = CompileVariableNames("RemovePrefix(string)");
        MethodInfo method = GetMethod(type, "RemovePrefix", BindingFlags.Public | BindingFlags.Static, typeof(string));

        // Act
        return method.InvokeStatic<string>(value);
    }

    [Test]
    [TestCaseSource(nameof(RemovePrefixCases))]
    public string RemovePrefix_ArrayBuilder(string value)
    {
        // Arrange
        Type type = CompileVariableNames($"RemovePrefix(string, {Generator.ARRAY_BUILDER}<char>)", $"{Generator.ARRAY_BUILDER}.ArrayBuilder()",
            $"{Generator.ARRAY_BUILDER}.ToString()");

        Type arrayBuilderType = type.Assembly.GetType($"{Generator.NAMESPACE}.ArrayBuilder`1", true)!.MakeGenericType(typeof(char));
        object arrayBuilderInstance = CreateInstance(arrayBuilderType);
        MethodInfo method = GetMethod(type, "RemovePrefix", BindingFlags.Public | BindingFlags.Static, typeof(string), arrayBuilderType);
        MethodInfo arrayBuilderToString = GetMethod(arrayBuilderType, "ToString", BindingFlags.Public | BindingFlags.Instance);

        // Act
        method.InvokeStatic<int>(value, arrayBuilderInstance);

        // Assert
        return arrayBuilderToString.InvokeInstance<string>(arrayBuilderInstance);
    }

    [Test]
    [TestCaseSource(nameof(UppercaseStartCases))]
    public string UppercaseStart_String(string value)
    {
        // Arrange
        Type type = CompileVariableNames("UppercaseStart(string)");
        MethodInfo method = GetMethod(type, "UppercaseStart", BindingFlags.Public | BindingFlags.Static, typeof(string));

        // Act
        return method.InvokeStatic<string>(value);
    }

    [Test]
    [TestCaseSource(nameof(UppercaseStartCases))]
    public string UppercaseStart_ArrayBuilder(string value)
    {
        // Arrange
        Type type = CompileVariableNames($"UppercaseStart(string, {Generator.ARRAY_BUILDER}<char>)", $"{Generator.ARRAY_BUILDER}.ArrayBuilder()",
            $"{Generator.ARRAY_BUILDER}.ToString()");

        Type arrayBuilderType = type.Assembly.GetType($"{Generator.NAMESPACE}.ArrayBuilder`1", true)!.MakeGenericType(typeof(char));
        object arrayBuilderInstance = CreateInstance(arrayBuilderType);
        MethodInfo method = GetMethod(type, "UppercaseStart", BindingFlags.Public | BindingFlags.Static, typeof(string), arrayBuilderType);
        MethodInfo arrayBuilderToString = GetMethod(arrayBuilderType, "ToString", BindingFlags.Public | BindingFlags.Instance);

        // Act
        method.InvokeStatic(value, arrayBuilderInstance);

        // Assert
        return arrayBuilderToString.InvokeInstance<string>(arrayBuilderInstance);
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
        Type type = CompileVariableNames("StartsWithOn(string)");
        MethodInfo method = GetMethod(type, "StartsWithOn", BindingFlags.Public | BindingFlags.Static, typeof(string));

        // Act
        return method.InvokeStatic<bool>(value);
    }

    [Test]
    [TestCaseSource(typeof(VariableNamesTests), nameof(GetNiceNameLengthCases))]
    public int GetNiceNameLength_String(string value)
    {
        // Arrange
        Type type = CompileVariableNames("GetNiceNameLength(string)");
        MethodInfo method = GetMethod(type, "GetNiceNameLength", BindingFlags.Public | BindingFlags.Static, typeof(string));

        // Act
        return method.InvokeStatic<int>(value);
    }

    private static Type CompileVariableNames(params string[] calledMethods)
    {
        return CompileGeneratedType("VariableNames", calledMethods);
    }
}