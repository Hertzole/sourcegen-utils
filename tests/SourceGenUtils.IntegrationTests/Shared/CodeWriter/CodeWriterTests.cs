using Bogus;
using Hertzole.SourceGen;

namespace Hertzole.SourceGenUtils.IntegrationTests;

internal partial class CodeWriterTests
{
    private CodeWriter writer = null!;

    private readonly Faker faker = new Faker();

    private const string APPEND_PREFIX = "This is the test message: ";

    private const int REPEATS = 100;

    [SetUp]
    public void Setup()
    {
        writer = new CodeWriter();
    }

    [TearDown]
    public void TearDown()
    {
        writer.Dispose();
    }

    [Test]
    [Repeat(REPEATS)]
    public void AppendNullable()
    {
        // Arrange
        string message = faker.Lorem.Sentence();
        string expected = $"""
                           #nullable enable
                           {message}
                           #nullable restore
                           """;

        // Act
        writer.AppendNullable().AppendLine(message);

        // Assert
        Assert.That(writer.ToString(), Is.EqualTo(expected));
    }

    [Test]
    [Repeat(REPEATS)]
    public void AppendNamespace([Values] bool newLine)
    {
        // Arrange
        string message = faker.Lorem.Sentence();
        const string nspace = "Test.Namespace";
        string expected = $$"""
                            namespace {{nspace}}
                            {
                                {{message}}
                            }
                            """;

        // Act
        writer.AppendNamespace(nspace);
        if (newLine)
        {
            writer.AppendLine(message);
        }
        else
        {
            writer.Append(message);
        }

        // Assert
        Assert.That(writer.ToString(), Is.EqualTo(expected));
    }

    [Test]
    [Repeat(REPEATS)]
    public void AppendGeneratedCodeAttribute()
    {
        // Arrange
        string message = faker.Lorem.Sentence();
        string generator = faker.Random.Word();
        string version = faker.System.Version().ToString();
        string expected = $"""
                           [global::System.CodeDom.Compiler.GeneratedCode("{generator}", "{version}")]
                           {message}
                           """;

        // Act
        writer.AppendGeneratedCodeAttribute(generator, version);
        writer.AppendLine(message);

        // Assert
        Assert.That(writer.ToString(), Is.EqualTo(expected));
    }

    [Test]
    [Repeat(REPEATS)]
    public void AppendExcludeFromCodeCoverageAttribute()
    {
        // Arrange
        string message = faker.Lorem.Sentence();
        string expected = $"""
                           [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                           {message}
                           """;

        // Act
        writer.AppendExcludeFromCodeCoverageAttribute();
        writer.AppendLine(message);

        // Assert
        Assert.That(writer.ToString(), Is.EqualTo(expected));
    }

    [Test]
    [Repeat(REPEATS)]
    public void AppendEmbeddedAttribute()
    {
        // Arrange
        string message = faker.Lorem.Sentence();
        string expected = $"""
                           [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
                           {message}
                           """;

        // Act
        writer.AppendEmbeddedAttribute();
        writer.AppendLine(message);

        // Assert
        Assert.That(writer.ToString(), Is.EqualTo(expected));
    }

    [Test]
    [Repeat(REPEATS)]
    [TestCase("#if TEST", "endif")]
    [TestCase("if TEST", "#endif")]
    public void AppendConditionalPreprocessorSymbol(string condition, string preprocessor)
    {
        // Arrange
        string message = faker.Lorem.Sentence();
        string expected = $"""
                           #if TEST
                           {message}
                           #endif
                           """;

        // Act
        writer.AppendConditionalSymbol(condition);
        writer.AppendLine(message);
        writer.AppendPreprocessorSymbol(preprocessor);

        // Assert
        Assert.That(writer.ToString(), Is.EqualTo(expected));
    }

    [Test]
    [Repeat(REPEATS)]
    public void Clear()
    {
        // Arrange
        writer.Append(faker.Lorem.Sentence());

        // Act
        writer.Clear();

        // Assert
        Assert.That(writer.ToString(), Is.Empty);
    }

    [Test]
    [Repeat(REPEATS)]
    public void WithBlock()
    {
        // Arrange
        string preMessage = faker.Lorem.Sentence();
        string message = faker.Lorem.Sentence();
        string expected = $$"""
                            {{preMessage}}
                            {
                                {{message}}
                            }
                            """;

        // Act
        writer.AppendLine(preMessage);
        using (writer.WithBlock())
        {
            writer.AppendLine(message);
        }

        // Assert
        Assert.That(writer.ToString(), Is.EqualTo(expected));
    }

    [Test]
    [Repeat(REPEATS)]
    public void WithIndent()
    {
        // Arrange
        string preMessage = faker.Lorem.Sentence();
        string message = faker.Lorem.Sentence();
        int originalIndent = writer.Indent;
        string expected = $"""
                           {preMessage}
                                   {message}
                           """;

        // Act
        writer.AppendLine(preMessage);
        using (writer.WithIndent(2))
        {
            writer.AppendLine(message);
        }

        // Assert
        Assert.That(writer.ToString(), Is.EqualTo(expected));
        Assert.That(writer.Indent, Is.EqualTo(originalIndent));
    }

    [Test]
    [Repeat(REPEATS)]
    [TestCase("TEST")]
    [TestCase("#if TEST")]
    [TestCase("if TEST")]
    [TestCase(" TEST")]
    public void WithCondition(string condition)
    {
        // Arrange
        string message = faker.Lorem.Sentence();
        string expected = $"""
                           #if TEST
                           {message}
                           #endif
                           """;

        // Act
        using (writer.WithCondition(condition))
        {
            writer.AppendLine(message);
        }

        // Assert
        Assert.That(writer.ToString(), Is.EqualTo(expected));
    }

    [Test]
    [Repeat(REPEATS)]
    public void ThrowsWhenDisposed()
    {
        // Arrange
        CodeWriter w = new CodeWriter();

        // Act
        w.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => w.AppendNullable(), "AppendNullable does not throw when disposed");

        Assert.Throws<ObjectDisposedException>(() => w.Append("test"), "Append(string?) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append("test".AsSpan()), "Append(ReadOnlySpan<char>) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append("test".AsMemory()), "Append(ReadOnlyMemory<char>) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append(new ArrayBuilder<char>()), "Append(ArrayBuilder<char>) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append('a'), "Append(char) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append('a', 5), "Append(char, int) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append(Array.Empty<char>()), "Append(char[]) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append(Array.Empty<char>(), 0, 1), "Append(char[], int, int) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append((byte) 0), "Append(byte) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append((byte) 0, string.Empty), "Append(byte, format, IFormatProvider) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append((sbyte) 0), "Append(sbyte) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append((sbyte) 0, string.Empty), "Append(sbyte, format, IFormatProvider) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append((short) 0), "Append(short) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append((short) 0, string.Empty), "Append(short, format, IFormatProvider) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append((ushort) 0), "Append(ushort) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append((ushort) 0, string.Empty),
            "Append(ushort, format, IFormatProvider) does not throw when disposed");

        Assert.Throws<ObjectDisposedException>(() => w.Append(0), "Append(int) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append(0, string.Empty), "Append(int, format, IFormatProvider) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append(0U), "Append(uint) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append(0U, string.Empty), "Append(uint, format, IFormatProvider) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append(0L), "Append(long) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append(0L, string.Empty), "Append(long, format, IFormatProvider) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append(0UL), "Append(ulong) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append(0UL, string.Empty), "Append(ulong, format, IFormatProvider) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append(0f), "Append(float) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append(0f, string.Empty), "Append(float, format, IFormatProvider) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append(0d), "Append(double) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append(0d, string.Empty), "Append(double, format, IFormatProvider) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append(0m), "Append(decimal) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append(0m, string.Empty), "Append(decimal, format, IFormatProvider) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append(true), "Append(bool) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Append((object) "test"), "Append(object) does not throw when disposed");

        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(), "AppendLine() does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine("test"), "AppendLine(string?) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine("test".AsSpan()), "AppendLine(ReadOnlySpan<char>) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine("test".AsMemory()), "AppendLine(ReadOnlyMemory<char>) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(new ArrayBuilder<char>()), "AppendLine(ArrayBuilder<char>) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine('a'), "AppendLine(char) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine('a', 5), "AppendLine(char, int) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(Array.Empty<char>(), 0, 1), "AppendLine(char[], int, int) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine((byte) 0), "AppendLine(byte) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine((byte) 0, string.Empty),
            "AppendLine(byte, format, IFormatProvider) does not throw when disposed");

        Assert.Throws<ObjectDisposedException>(() => w.AppendLine((sbyte) 0), "AppendLine(sbyte) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine((sbyte) 0, string.Empty),
            "AppendLine(sbyte, format, IFormatProvider) does not throw when disposed");

        Assert.Throws<ObjectDisposedException>(() => w.AppendLine((short) 0), "AppendLine(short) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine((short) 0, string.Empty),
            "AppendLine(short, format, IFormatProvider) does not throw when disposed");

        Assert.Throws<ObjectDisposedException>(() => w.AppendLine((ushort) 0), "AppendLine(ushort) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine((ushort) 0, string.Empty),
            "AppendLine(ushort, format, IFormatProvider) does not throw when disposed");

        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(0), "AppendLine(int) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(0, string.Empty), "AppendLine(int, format, IFormatProvider) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(0U), "AppendLine(uint) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(0U, string.Empty), "AppendLine(uint, format, IFormatProvider) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(0L), "AppendLine(long) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(0L, string.Empty), "AppendLine(long, format, IFormatProvider) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(0UL), "AppendLine(ulong) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(0UL, string.Empty),
            "AppendLine(ulong, format, IFormatProvider) does not throw when disposed");

        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(0f), "AppendLine(float) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(0f, string.Empty), "AppendLine(float, format, IFormatProvider) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(0d), "AppendLine(double) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(0d, string.Empty),
            "AppendLine(double, format, IFormatProvider) does not throw when disposed");

        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(0m), "AppendLine(decimal) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(0m, string.Empty),
            "AppendLine(decimal, format, IFormatProvider) does not throw when disposed");

        Assert.Throws<ObjectDisposedException>(() => w.AppendLine(true), "AppendLine(bool) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendLine((object) "test"), "AppendLine(object) does not throw when disposed");

        Assert.Throws<ObjectDisposedException>(() => w.AppendNamespace("Test"), "AppendNamespace(string) does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendGeneratedCodeAttribute("test", "1.0"),
            "AppendGeneratedCodeAttribute does not throw when disposed");

        Assert.Throws<ObjectDisposedException>(() => w.AppendExcludeFromCodeCoverageAttribute(),
            "AppendExcludeFromCodeCoverageAttribute does not throw when disposed");

        Assert.Throws<ObjectDisposedException>(() => w.AppendEmbeddedAttribute(), "AppendEmbeddedAttribute does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendConditionalSymbol("TEST"), "AppendConditionalSymbol does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.AppendPreprocessorSymbol("endif"), "AppendPreprocessorSymbol does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.Clear(), "Clear does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => _ = w.ToString(), "ToString does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.WithBlock(), "WithBlock does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.WithIndent(2), "WithIndent does not throw when disposed");
        Assert.Throws<ObjectDisposedException>(() => w.WithCondition("TEST"), "WithCondition does not throw when disposed");
    }
}