using System;
using System.Text;
using Hertzole.SourceGen.Wrappers;
using NUnit.Framework;

namespace SourceGenUtils.Tests;

public class StringBuilderExtensionsTests : GeneratorTests
{
    /// <inheritdoc />
    protected override string GetTypeName()
    {
        return "StringBuilderExtensions";
    }

    [Test]
    [TestCase(false, TestName = "Safe")]
    [TestCase(true, TestName = "Unsafe")]
    public void Append(bool allowUnsafe)
    {
        // Arrange
        StringBuilderExtensions extensions = GetWrapper("StringBuilderExtensions.Append(null, default);", allowUnsafe);
        StringBuilder stringBuilder = new StringBuilder();
        ReadOnlySpan<char> value = Fake.Lorem.Sentences().AsSpan();

        // Act
        StringBuilder returned = extensions.Append(stringBuilder, value);

        // Assert
        Assert.That(returned, Is.SameAs(stringBuilder));
        Assert.That(stringBuilder.ToString(), Is.EqualTo(value.ToString()));
    }

    [Test]
    [TestCase(false, TestName = "Safe")]
    [TestCase(true, TestName = "Unsafe")]
    public void AppendLine(bool allowUnsafe)
    {
        // Arrange
        StringBuilderExtensions extensions = GetWrapper("StringBuilderExtensions.AppendLine(null, default);", allowUnsafe);
        StringBuilder stringBuilder = new StringBuilder();
        ReadOnlySpan<char> value1 = Fake.Lorem.Sentences().AsSpan();
        ReadOnlySpan<char> value2 = Fake.Lorem.Sentences().AsSpan();
        string expected = new StringBuilder().AppendLine(value1.ToString()).AppendLine(value2.ToString()).ToString();

        // Act
        StringBuilder returned = extensions.AppendLine(stringBuilder, value1);
        returned = extensions.AppendLine(returned, value2);

        // Assert
        Assert.That(returned, Is.SameAs(stringBuilder));
        Assert.That(stringBuilder.ToString(), Is.EqualTo(expected));
    }

    private static StringBuilderExtensions GetWrapper(string useMethod, bool allowUnsafe)
    {
        Type type = AssemblyBuilder.CompileGeneratedTypeByUsing("StringBuilderExtensions", useMethod, allowUnsafe);
        return new StringBuilderExtensions(type);
    }
}