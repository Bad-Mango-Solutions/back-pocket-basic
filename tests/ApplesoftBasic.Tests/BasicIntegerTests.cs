// <copyright file="BasicIntegerTests.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Tests;

using ApplesoftBasic.Interpreter.Emulation;

/// <summary>
/// Contains unit tests for the <see cref="BasicInteger"/> struct.
/// </summary>
[TestFixture]
public class BasicIntegerTests
{
    #region Creation Tests

    /// <summary>
    /// Verifies that BasicInteger correctly stores a positive value.
    /// </summary>
    [Test]
    public void Constructor_PositiveValue_StoresCorrectly()
    {
        BasicInteger value = 42;

        Assert.That(value.Value, Is.EqualTo(42));
        Assert.That(value.IsZero, Is.False);
        Assert.That(value.IsNegative, Is.False);
    }

    /// <summary>
    /// Verifies that BasicInteger correctly stores a negative value.
    /// </summary>
    [Test]
    public void Constructor_NegativeValue_StoresCorrectly()
    {
        BasicInteger value = -42;

        Assert.That(value.Value, Is.EqualTo(-42));
        Assert.That(value.IsZero, Is.False);
        Assert.That(value.IsNegative, Is.True);
    }

    /// <summary>
    /// Verifies that BasicInteger correctly stores zero.
    /// </summary>
    [Test]
    public void Constructor_Zero_StoresCorrectly()
    {
        BasicInteger value = 0;

        Assert.That(value.Value, Is.EqualTo(0));
        Assert.That(value.IsZero, Is.True);
        Assert.That(value.IsNegative, Is.False);
    }

    /// <summary>
    /// Verifies that BasicInteger correctly stores maximum value.
    /// </summary>
    [Test]
    public void Constructor_MaxValue_StoresCorrectly()
    {
        BasicInteger value = BasicInteger.MaxValue;

        Assert.That(value.Value, Is.EqualTo(32767));
    }

    /// <summary>
    /// Verifies that BasicInteger correctly stores minimum value.
    /// </summary>
    [Test]
    public void Constructor_MinValue_StoresCorrectly()
    {
        BasicInteger value = BasicInteger.MinValue;

        Assert.That(value.Value, Is.EqualTo(-32768));
    }

    /// <summary>
    /// Verifies that overflow throws exception.
    /// </summary>
    [Test]
    public void ImplicitConversion_Overflow_ThrowsException()
    {
        Assert.Throws<OverflowException>(() =>
        {
            BasicInteger value = 32768;
        });
    }

    /// <summary>
    /// Verifies that underflow throws exception.
    /// </summary>
    [Test]
    public void ImplicitConversion_Underflow_ThrowsException()
    {
        Assert.Throws<OverflowException>(() =>
        {
            BasicInteger value = -32769;
        });
    }

    #endregion

    #region FromDouble Tests

    /// <summary>
    /// Verifies that FromDouble truncates positive values correctly.
    /// </summary>
    [Test]
    public void FromDouble_PositiveWithFraction_TruncatesTowardZero()
    {
        var value = BasicInteger.FromDouble(3.7);
        Assert.That(value.Value, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that FromDouble truncates negative values correctly.
    /// </summary>
    [Test]
    public void FromDouble_NegativeWithFraction_TruncatesTowardZero()
    {
        var value = BasicInteger.FromDouble(-3.7);
        Assert.That(value.Value, Is.EqualTo(-3));
    }

    /// <summary>
    /// Verifies that FromDouble handles exact integers.
    /// </summary>
    [Test]
    public void FromDouble_ExactInteger_ConvertsCorrectly()
    {
        var value = BasicInteger.FromDouble(42.0);
        Assert.That(value.Value, Is.EqualTo(42));
    }

    /// <summary>
    /// Verifies that FromDouble throws on overflow.
    /// </summary>
    [Test]
    public void FromDouble_Overflow_ThrowsException()
    {
        Assert.Throws<OverflowException>(() => BasicInteger.FromDouble(40000.0));
    }

    #endregion

    #region Byte Conversion Tests

    /// <summary>
    /// Verifies that ToBytes produces correct little-endian format.
    /// </summary>
    [Test]
    public void ToBytes_PositiveValue_ReturnsLittleEndian()
    {
        BasicInteger value = 0x1234; // 4660 in decimal
        byte[] bytes = value.ToBytes();

        Assert.That(bytes.Length, Is.EqualTo(2));
        Assert.That(bytes[0], Is.EqualTo(0x34)); // Low byte
        Assert.That(bytes[1], Is.EqualTo(0x12)); // High byte
    }

    /// <summary>
    /// Verifies that ToBytes handles negative values correctly.
    /// </summary>
    [Test]
    public void ToBytes_NegativeValue_ReturnsTwosComplement()
    {
        BasicInteger value = -1;
        byte[] bytes = value.ToBytes();

        Assert.That(bytes[0], Is.EqualTo(0xFF));
        Assert.That(bytes[1], Is.EqualTo(0xFF));
    }

    /// <summary>
    /// Verifies that FromBytes reads little-endian format.
    /// </summary>
    [Test]
    public void FromBytes_LittleEndian_ReadsCorrectly()
    {
        byte[] bytes = new byte[] { 0x34, 0x12 };
        var value = BasicInteger.FromBytes(bytes);

        Assert.That(value.Value, Is.EqualTo(0x1234));
    }

    /// <summary>
    /// Verifies round-trip through bytes.
    /// </summary>
    /// <param name="original">The original value.</param>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(-1)]
    [TestCase(32767)]
    [TestCase(-32768)]
    [TestCase(1000)]
    [TestCase(-1000)]
    public void ByteRoundTrip_PreservesValue(int original)
    {
        BasicInteger value = (short)original;
        byte[] bytes = value.ToBytes();
        var restored = BasicInteger.FromBytes(bytes);

        Assert.That(restored.Value, Is.EqualTo(original));
    }

    /// <summary>
    /// Verifies that FromBytes with null throws exception.
    /// </summary>
    [Test]
    public void FromBytes_Null_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() => BasicInteger.FromBytes(null!));
    }

    /// <summary>
    /// Verifies that FromBytes with short array throws exception.
    /// </summary>
    [Test]
    public void FromBytes_TooShort_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => BasicInteger.FromBytes(new byte[] { 0x00 }));
    }

    #endregion

    #region Equality Tests

    /// <summary>
    /// Verifies that equal values are equal.
    /// </summary>
    [Test]
    public void Equality_SameValue_ReturnsTrue()
    {
        BasicInteger a = 42;
        BasicInteger b = 42;

        Assert.That(a == b, Is.True);
        Assert.That(a.Equals(b), Is.True);
    }

    /// <summary>
    /// Verifies that different values are not equal.
    /// </summary>
    [Test]
    public void Equality_DifferentValue_ReturnsFalse()
    {
        BasicInteger a = 42;
        BasicInteger b = 43;

        Assert.That(a == b, Is.False);
        Assert.That(a != b, Is.True);
    }

    #endregion

    #region Conversion Tests

    /// <summary>
    /// Verifies ToDouble conversion.
    /// </summary>
    [Test]
    public void ToDouble_ConvertsCorrectly()
    {
        BasicInteger value = 42;
        Assert.That(value.ToDouble(), Is.EqualTo(42.0));
    }

    /// <summary>
    /// Verifies ToMbf conversion.
    /// </summary>
    [Test]
    public void ToMbf_ConvertsCorrectly()
    {
        BasicInteger value = 42;
        MBF mbf = value.ToMbf();
        Assert.That(mbf.ToDouble(), Is.EqualTo(42.0));
    }

    #endregion
}