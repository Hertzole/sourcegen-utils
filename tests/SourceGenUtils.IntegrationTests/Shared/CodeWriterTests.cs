using System.Globalization;
using Bogus;
using Hertzole.SourceGen;

namespace Hertzole.SourceGenUtils.IntegrationTests;

public class CodeWriterTests
{
    private CodeWriter writer = null!;

    private readonly Faker faker = new Faker();

    private const string APPEND_PREFIX = "This is the test message: ";

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
    public void AppendString()
    {
        // Arrange
        string message = faker.Lorem.Sentence();

        // Act
        AppendTest(w => w.Append(message), message);
    }

    [Test]
    public void AppendReadOnlySpan()
    {
        // Arrange
        string message = faker.Lorem.Sentence();

        // Act
        AppendTest(w => w.Append(message.AsSpan()), message);
    }

    [Test]
    public void AppendReadOnlyMemory()
    {
        // Arrange
        string message = faker.Lorem.Sentence();

        // Act
        AppendTest(w => w.Append(message.AsMemory()), message);
    }

    [Test]
    public void AppendChar()
    {
        // Arrange
        char value = faker.Random.Char();

        // Act
        AppendTest(w => w.Append(value), value.ToString());
    }

    [Test]
    public void AppendCharRepeat()
    {
        // Arrange
        char value = faker.Random.Char();
        int repeat = faker.Random.Int(5, 10);
        string expected = new string(value, repeat);

        // Act
        AppendTest(w => w.Append(value, repeat), expected);
    }

    [Test]
    public void AppendCharArray()
    {
        // Arrange
        char[] value = faker.Random.Chars();
        string expected = new string(value);

        // Act
        AppendTest(w => w.Append(value), expected);
    }

    [Test]
    public void AppendCharArraySpan()
    {
        // Arrange
        char[] value = faker.Random.Chars(count: 32);
        int start = faker.Random.Int(2, 7);
        int count = faker.Random.Int(3, 5);
        string expected = value.AsSpan(start, count).ToString();

        // Act
        AppendTest(w => w.Append(value, start, count), expected);
    }

    [Test]
    public void AppendByte()
    {
        // Arrange
        byte value = faker.Random.Byte();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendByteFormat()
    {
        // Arrange
        byte value = faker.Random.Byte();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendTest(w => w.Append(value, format, culture), expected);
    }

    [Test]
    public void AppendSByte()
    {
        // Arrange
        sbyte value = faker.Random.SByte();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendSByteFormat()
    {
        // Arrange
        sbyte value = faker.Random.SByte();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendTest(w => w.Append(value, format, culture), expected);
    }

    [Test]
    public void AppendShort()
    {
        // Arrange
        short value = faker.Random.Short();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendShortFormat()
    {
        // Arrange
        short value = faker.Random.Short();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendTest(w => w.Append(value, format, culture), expected);
    }

    [Test]
    public void AppendUShort()
    {
        // Arrange
        ushort value = faker.Random.UShort();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendUShortFormat()
    {
        // Arrange
        ushort value = faker.Random.UShort();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendTest(w => w.Append(value, format, culture), expected);
    }

    [Test]
    public void AppendInt()
    {
        // Arrange
        int value = faker.Random.Int();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendIntFormat()
    {
        // Arrange
        int value = faker.Random.Int();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendTest(w => w.Append(value, format, culture), expected);
    }

    [Test]
    public void AppendUInt()
    {
        // Arrange
        uint value = faker.Random.UInt();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendUIntFormat()
    {
        // Arrange
        uint value = faker.Random.UInt();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendTest(w => w.Append(value, format, culture), expected);
    }

    [Test]
    public void AppendLong()
    {
        // Arrange
        long value = faker.Random.Long();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendLongFormat()
    {
        // Arrange
        long value = faker.Random.Long();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendTest(w => w.Append(value, format, culture), expected);
    }

    [Test]
    public void AppendULong()
    {
        // Arrange
        ulong value = faker.Random.ULong();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendULongFormat()
    {
        // Arrange
        ulong value = faker.Random.ULong();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendTest(w => w.Append(value, format, culture), expected);
    }

    [Test]
    public void AppendFloat()
    {
        // Arrange
        float value = faker.Random.Float();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendFloatFormat()
    {
        // Arrange
        float value = faker.Random.Float();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendTest(w => w.Append(value, format, culture), expected);
    }

    [Test]
    public void AppendDouble()
    {
        // Arrange
        double value = faker.Random.Double();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendDoubleFormat()
    {
        // Arrange
        double value = faker.Random.Double();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendTest(w => w.Append(value, format, culture), expected);
    }

    [Test]
    public void AppendDecimal()
    {
        // Arrange
        decimal value = faker.Random.Decimal();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendDecimalFormat()
    {
        // Arrange
        decimal value = faker.Random.Decimal();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendTest(w => w.Append(value, format, culture), expected);
    }

    [Test]
    public void AppendBool([Values] bool value)
    {
        // Arrange
        string expected = value ? "true" : "false";

        // Act
        AppendTest(w => w.Append(value), expected);
    }

    [Test]
    public void AppendObject()
    {
        // Arrange
        string value = faker.Random.Word();

        // Act
        AppendTest(w => w.Append((object) value), value);
    }

    [Test]
    public void AppendLineEmpty()
    {
        // Arrange
        string line1 = faker.Lorem.Sentence();
        string line2 = faker.Lorem.Sentence();
        string expected = $""""
                           {line1}
                           {line2}
                           """";

        // Act
        writer.Append(line1);
        writer.AppendLine();
        writer.Append(line2);

        // Assert
        Assert.That(writer.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public void AppendLineString()
    {
        // Arrange
        string message = faker.Lorem.Sentence();

        // Act
        AppendLineTest(w => w.AppendLine(message), message);
    }

    [Test]
    public void AppendLineReadOnlySpan()
    {
        // Arrange
        string message = faker.Lorem.Sentence();

        // Act
        AppendLineTest(w => w.AppendLine(message.AsSpan()), message);
    }

    [Test]
    public void AppendLineChar()
    {
        // Arrange
        char value = faker.Random.Char();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString());
    }

    [Test]
    public void AppendLineCharRepeat()
    {
        // Arrange
        char value = faker.Random.Char();
        int repeat = faker.Random.Int(5, 10);
        string expected = new string(value, repeat);

        // Act
        AppendLineTest(w => w.AppendLine(value, repeat), expected);
    }

    [Test]
    public void AppendLineCharArraySpan()
    {
        // Arrange
        char[] value = faker.Random.Chars(count: 32);
        int start = faker.Random.Int(2, 7);
        int count = faker.Random.Int(3, 5);
        string expected = value.AsSpan(start, count).ToString();

        // Act
        AppendLineTest(w => w.AppendLine(value, start, count), expected);
    }

    [Test]
    public void AppendLineByte()
    {
        // Arrange
        byte value = faker.Random.Byte();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendLineByteFormat()
    {
        // Arrange
        byte value = faker.Random.Byte();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendLineTest(w => w.AppendLine(value, format, culture), expected);
    }

    [Test]
    public void AppendLineSByte()
    {
        // Arrange
        sbyte value = faker.Random.SByte();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendLineSByteFormat()
    {
        // Arrange
        sbyte value = faker.Random.SByte();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendLineTest(w => w.AppendLine(value, format, culture), expected);
    }

    [Test]
    public void AppendLineShort()
    {
        // Arrange
        short value = faker.Random.Short();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendLineShortFormat()
    {
        // Arrange
        short value = faker.Random.Short();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendLineTest(w => w.AppendLine(value, format, culture), expected);
    }

    [Test]
    public void AppendLineUShort()
    {
        // Arrange
        ushort value = faker.Random.UShort();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendLineUShortFormat()
    {
        // Arrange
        ushort value = faker.Random.UShort();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendLineTest(w => w.AppendLine(value, format, culture), expected);
    }

    [Test]
    public void AppendLineInt()
    {
        // Arrange
        int value = faker.Random.Int();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendLineIntFormat()
    {
        // Arrange
        int value = faker.Random.Int();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendLineTest(w => w.AppendLine(value, format, culture), expected);
    }

    [Test]
    public void AppendLineUInt()
    {
        // Arrange
        uint value = faker.Random.UInt();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendLineUIntFormat()
    {
        // Arrange
        uint value = faker.Random.UInt();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendLineTest(w => w.AppendLine(value, format, culture), expected);
    }

    [Test]
    public void AppendLineLong()
    {
        // Arrange
        long value = faker.Random.Long();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendLineLongFormat()
    {
        // Arrange
        long value = faker.Random.Long();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendLineTest(w => w.AppendLine(value, format, culture), expected);
    }

    [Test]
    public void AppendLineULong()
    {
        // Arrange
        ulong value = faker.Random.ULong();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendLineULongFormat()
    {
        // Arrange
        ulong value = faker.Random.ULong();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendLineTest(w => w.AppendLine(value, format, culture), expected);
    }

    [Test]
    public void AppendLineFloat()
    {
        // Arrange
        float value = faker.Random.Float();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendLineFloatFormat()
    {
        // Arrange
        float value = faker.Random.Float();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendLineTest(w => w.AppendLine(value, format, culture), expected);
    }

    [Test]
    public void AppendLineDouble()
    {
        // Arrange
        double value = faker.Random.Double();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendLineDoubleFormat()
    {
        // Arrange
        double value = faker.Random.Double();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendLineTest(w => w.AppendLine(value, format, culture), expected);
    }

    [Test]
    public void AppendLineDecimal()
    {
        // Arrange
        decimal value = faker.Random.Decimal();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    public void AppendLineDecimalFormat()
    {
        // Arrange
        decimal value = faker.Random.Decimal();
        string format = "P1";
        CultureInfo culture = faker.PickRandom(CultureInfo.GetCultures(CultureTypes.AllCultures));
        string expected = value.ToString(format, culture);

        // Act
        AppendLineTest(w => w.AppendLine(value, format, culture), expected);
    }

    [Test]
    public void AppendLineBool([Values] bool value)
    {
        // Arrange
        bool value1 = value;
        bool value2 = !value;
        string expected = $"""
                           {value1.ToString().ToLowerInvariant()}
                           {value2.ToString().ToLowerInvariant()}
                           """;

        // Act
        AppendLineTest(w => w.AppendLine(value1).AppendLine(value2), expected);
    }

    [Test]
    public void AppendLineObject()
    {
        // Arrange
        string value = faker.Random.Word();

        // Act
        AppendLineTest(w => w.AppendLine((object) value), value);
    }

    [Test]
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

    private void AppendTest(Action<CodeWriter> write, string expectedMessage)
    {
        // Arrange
        string expected = $"{APPEND_PREFIX}{expectedMessage}";

        // Act
        writer.Append(APPEND_PREFIX);
        write.Invoke(writer);

        // Assert
        Assert.That(writer.ToString(), Is.EqualTo(expected));
    }

    private void AppendLineTest(Action<CodeWriter> write, string expectedMessage)
    {
        // Arrange
        string expected = $"{APPEND_PREFIX}{expectedMessage}";

        // Act
        writer.Append(APPEND_PREFIX);
        write.Invoke(writer);

        // Append another line to verify newline was written
        writer.AppendLine("suffix");
        string result = writer.ToString();

        // Assert
        Assert.That(result, Is.EqualTo($"{expected}\nsuffix"));
    }
}