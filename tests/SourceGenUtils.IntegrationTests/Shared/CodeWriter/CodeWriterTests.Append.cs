using System.Globalization;
using Hertzole.SourceGen;

namespace Hertzole.SourceGenUtils.IntegrationTests;

internal partial class CodeWriterTests
{
    [Test]
    [Repeat(REPEATS)]
    public void AppendString()
    {
        // Arrange
        string message = faker.Lorem.Sentence();

        // Act
        AppendTest(w => w.Append(message), message);
    }

    [Test]
    [Repeat(REPEATS)]
    public void AppendReadOnlySpan()
    {
        // Arrange
        string message = faker.Lorem.Sentence();

        // Act
        AppendTest(w => w.Append(message.AsSpan()), message);
    }

    [Test]
    [Repeat(REPEATS)]
    public void AppendReadOnlyMemory()
    {
        // Arrange
        string message = faker.Lorem.Sentence();

        // Act
        AppendTest(w => w.Append(message.AsMemory()), message);
    }

    [Test]
    [Repeat(REPEATS)]
    public void AppendArrayBuilder()
    {
        // Arrange
        string message = faker.Lorem.Sentence();

        // Act
        AppendTest(w =>
        {
            using (ArrayBuilder<char> builder = new ArrayBuilder<char>())
            {
                builder.AddRange(message);
                w.Append(builder);
            }
        }, message);
    }

    [Test]
    [Repeat(REPEATS)]
    public void AppendChar()
    {
        // Arrange
        char value = faker.Random.Char();

        // Act
        AppendTest(w => w.Append(value), value.ToString());
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendCharArray()
    {
        // Arrange
        char[] value = faker.Random.Chars();
        string expected = new string(value);

        // Act
        AppendTest(w => w.Append(value), expected);
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendByte()
    {
        // Arrange
        byte value = faker.Random.Byte();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendSByte()
    {
        // Arrange
        sbyte value = faker.Random.SByte();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendShort()
    {
        // Arrange
        short value = faker.Random.Short();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendUShort()
    {
        // Arrange
        ushort value = faker.Random.UShort();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendInt()
    {
        // Arrange
        int value = faker.Random.Int();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendUInt()
    {
        // Arrange
        uint value = faker.Random.UInt();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendLong()
    {
        // Arrange
        long value = faker.Random.Long();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendULong()
    {
        // Arrange
        ulong value = faker.Random.ULong();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendFloat()
    {
        // Arrange
        float value = faker.Random.Float();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendDouble()
    {
        // Arrange
        double value = faker.Random.Double();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendDecimal()
    {
        // Arrange
        decimal value = faker.Random.Decimal();

        // Act
        AppendTest(w => w.Append(value), value.ToString("G", CultureInfo.InvariantCulture));
    }

    [Test]
    [Repeat(REPEATS)]
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
    [Repeat(REPEATS)]
    public void AppendBool([Values] bool value)
    {
        // Arrange
        string expected = value ? "true" : "false";

        // Act
        AppendTest(w => w.Append(value), expected);
    }

    [Test]
    [Repeat(REPEATS)]
    public void AppendObject()
    {
        // Arrange
        string value = faker.Random.Word();

        // Act
        AppendTest(w => w.Append((object) value), value);
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
}