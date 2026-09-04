using System;
using System.Reflection;
using System.Text;
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
        StringBuilderExtensions extensions = new StringBuilderExtensions(allowUnsafe, "Append(System.Text.StringBuilder, System.ReadOnlySpan<char>)");
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
        StringBuilderExtensions extensions = new StringBuilderExtensions(allowUnsafe, "AppendLine(System.Text.StringBuilder, System.ReadOnlySpan<char>)");
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

    private class StringBuilderExtensions
    {
        private readonly Func<StringBuilder, ReadOnlySpan<char>, StringBuilder> append;
        private readonly Func<StringBuilder, ReadOnlySpan<char>, StringBuilder> appendLine;

        public StringBuilderExtensions(bool allowUnsafe, params string[] calledMethods)
        {
            Type type = allowUnsafe
                ? CompileUnsafeGeneratedType("StringBuilderExtensions", calledMethods)
                : CompileGeneratedType("StringBuilderExtensions", calledMethods);

            append = (Func<StringBuilder, ReadOnlySpan<char>, StringBuilder>) Delegate.CreateDelegate(
                typeof(Func<StringBuilder, ReadOnlySpan<char>, StringBuilder>),
                GetMethod(type, "Append", BindingFlags.Public | BindingFlags.Static, typeof(StringBuilder), typeof(ReadOnlySpan<char>)));

            appendLine = (Func<StringBuilder, ReadOnlySpan<char>, StringBuilder>) Delegate.CreateDelegate(
                typeof(Func<StringBuilder, ReadOnlySpan<char>, StringBuilder>),
                GetMethod(type, "AppendLine", BindingFlags.Public | BindingFlags.Static, typeof(StringBuilder), typeof(ReadOnlySpan<char>)));
        }

        public StringBuilder Append(StringBuilder stringBuilder, ReadOnlySpan<char> value)
        {
            return append(stringBuilder, value);
        }

        public StringBuilder AppendLine(StringBuilder stringBuilder, ReadOnlySpan<char> value)
        {
            return appendLine(stringBuilder, value);
        }
    }
}