using System.Globalization;
using Hertzole.SourceGen;

namespace Hertzole.SourceGenUtils.IntegrationTests;

internal partial class CodeWriterTests
{
    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendLineString()
    {
        // Arrange
        string message = faker.Lorem.Sentence();

        // Act
        AppendLineTest(w => w.AppendLine(message), message);
    }

    [Test]
    [Repeat(REPEATS)]
    public void AppendLineReadOnlySpan()
    {
        // Arrange
        string message = faker.Lorem.Sentence();

        // Act
        AppendLineTest(w => w.AppendLine(message.AsSpan()), message);
    }

    [Test]
    [Repeat(REPEATS)]
    public void AppendLineReadOnlyMemory()
    {
        // Arrange
        string message = faker.Lorem.Sentence();

        // Act
        AppendLineTest(w => w.AppendLine(message.AsMemory()), message);
    }

    [Test]
    [Repeat(REPEATS)]
    public void AppendLineArrayBuilder()
    {
        // Arrange
        string message = faker.Lorem.Sentence();

        // Act
        AppendLineTest(w =>
        {
            using ArrayBuilder<char> builder = new ArrayBuilder<char>();
            builder.AddRange(message);
            w.AppendLine(builder);
        }, message);
    }

    [Test]
    [Repeat(REPEATS)]
    public void AppendLineChar()
    {
        // Arrange
        char value = faker.Random.Char();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString());
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendLineByte()
    {
        // Arrange
        byte value = faker.Random.Byte();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendLineSByte()
    {
        // Arrange
        sbyte value = faker.Random.SByte();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendLineShort()
    {
        // Arrange
        short value = faker.Random.Short();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendLineUShort()
    {
        // Arrange
        ushort value = faker.Random.UShort();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendLineInt()
    {
        // Arrange
        int value = faker.Random.Int();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendLineUInt()
    {
        // Arrange
        uint value = faker.Random.UInt();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendLineLong()
    {
        // Arrange
        long value = faker.Random.Long();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendLineULong()
    {
        // Arrange
        ulong value = faker.Random.ULong();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendLineFloat()
    {
        // Arrange
        float value = faker.Random.Float();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendLineDouble()
    {
        // Arrange
        double value = faker.Random.Double();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendLineDecimal()
    {
        // Arrange
        decimal value = faker.Random.Decimal();

        // Act
        AppendLineTest(w => w.AppendLine(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendLineObject()
    {
        // Arrange
        string value = faker.Random.Word();

        // Act
        AppendLineTest(w => w.AppendLine((object) value), value);
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